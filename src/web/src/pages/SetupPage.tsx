import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { completeSetup } from '../api/setup';
import { useAuth } from '../context/AuthContext';

type Step = 'admin' | 'settings' | 'done';

export default function SetupPage() {
  const navigate = useNavigate();
  const { setTokenAndUser } = useAuth();
  const [step, setStep] = useState<Step>('admin');
  const [error, setError] = useState('');

  // Admin fields
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  // Settings fields
  const [anthropicApiKey, setAnthropicApiKey] = useState('');
  const [aiAnalysisPrompt, setAiAnalysisPrompt] = useState('');

  function handleAdminNext(e: FormEvent) {
    e.preventDefault();
    setError('');
    if (password !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }
    if (password.length < 8) {
      setError('Password must be at least 8 characters.');
      return;
    }
    setStep('settings');
  }

  async function handleFinish(e: FormEvent) {
    e.preventDefault();
    setError('');
    try {
      const res = await completeSetup({
        username,
        email,
        password,
        ...(anthropicApiKey && { anthropicApiKey }),
        ...(aiAnalysisPrompt && { aiAnalysisPrompt }),
      });
      setTokenAndUser(res.token, { username: res.username, email: res.email, role: res.role });
      setStep('done');
    } catch {
      setError('Setup failed. Please try again.');
    }
  }

  if (step === 'done') {
    return (
      <div className="setup-wizard">
        <h1>✅ Setup Complete</h1>
        <p>Your WatchTracker instance is ready to use.</p>
        <button className="btn" onClick={() => navigate('/')}>Go to Dashboard</button>
      </div>
    );
  }

  return (
    <div className="setup-wizard">
      <h1>⌚ WatchTracker Setup</h1>
      <div className="setup-steps">
        <span className={step === 'admin' ? 'step active' : 'step'}>1. Admin Account</span>
        <span className={step === 'settings' ? 'step active' : 'step'}>2. Settings</span>
      </div>

      {error && <p className="error">{error}</p>}

      {step === 'admin' && (
        <form className="setup-form" onSubmit={handleAdminNext}>
          <p>Create the administrator account for your WatchTracker instance.</p>
          <label>
            Username *
            <input value={username} onChange={(e) => setUsername(e.target.value)} required />
          </label>
          <label>
            Email *
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </label>
          <label>
            Password *
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
          </label>
          <label>
            Confirm Password *
            <input type="password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} required />
          </label>
          <button type="submit" className="btn">Next →</button>
        </form>
      )}

      {step === 'settings' && (
        <form className="setup-form" onSubmit={handleFinish}>
          <p>Configure optional settings. You can change these later in the admin panel.</p>
          <label>
            Anthropic API Key
            <input
              type="password"
              value={anthropicApiKey}
              onChange={(e) => setAnthropicApiKey(e.target.value)}
              placeholder="sk-ant-..."
            />
            <small>Required for AI watch analysis. Can be added later.</small>
          </label>
          <label>
            AI Analysis Prompt
            <textarea
              value={aiAnalysisPrompt}
              onChange={(e) => setAiAnalysisPrompt(e.target.value)}
              placeholder="Leave blank for default prompt"
              rows={4}
            />
            <small>Custom prompt for the AI watch analysis feature.</small>
          </label>
          <div className="setup-nav">
            <button type="button" className="btn" onClick={() => setStep('admin')}>← Back</button>
            <button type="submit" className="btn">Finish Setup</button>
          </div>
        </form>
      )}
    </div>
  );
}
