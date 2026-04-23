import { useEffect, useRef, useState, type FormEvent } from 'react';
import { getUsers, unlockUser, resetUserPassword, getSettings, updateSettings, getOllamaModels } from '../api/admin';
import type { UserDto } from '../types';

type Tab = 'users' | 'settings';

export default function AdminPage() {
  const [tab, setTab] = useState<Tab>('users');

  return (
    <div className="admin-page">
      <h1>Admin Panel</h1>
      <div className="admin-tabs">
        <button className={`admin-tab${tab === 'users' ? ' active' : ''}`} onClick={() => setTab('users')}>Users</button>
        <button className={`admin-tab${tab === 'settings' ? ' active' : ''}`} onClick={() => setTab('settings')}>Settings</button>
      </div>
      {tab === 'users' ? <UsersPanel /> : <SettingsPanel />}
      <div className="build-info">
        <span>Build: {__BUILD_SHA__}</span>
        <span>{new Date(__BUILD_DATE__).toLocaleString()}</span>
      </div>
    </div>
  );
}

function UsersPanel() {
  const [users, setUsers] = useState<UserDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [resetId, setResetId] = useState<number | null>(null);
  const [newPassword, setNewPassword] = useState('');

  useEffect(() => {
    loadUsers();
  }, []);

  async function loadUsers() {
    setLoading(true);
    try {
      setUsers(await getUsers());
    } finally {
      setLoading(false);
    }
  }

  async function handleUnlock(id: number) {
    await unlockUser(id);
    await loadUsers();
  }

  async function handleResetPassword(e: FormEvent) {
    e.preventDefault();
    if (resetId === null || !newPassword) return;
    await resetUserPassword(resetId, newPassword);
    setResetId(null);
    setNewPassword('');
    await loadUsers();
  }

  if (loading) return <p>Loading…</p>;

  return (
    <div className="users-panel">
      {users.map((u) => (
        <fieldset key={u.id} className="watch-form-group user-card">
          <legend>{u.username}</legend>
          <div className="watch-form-row">
            <label>
              Email
              <span className="user-card-value">{u.email}</span>
            </label>
            <label>
              Role
              <span><span className={`role-badge role-${u.role.toLowerCase()}`}>{u.role}</span></span>
            </label>
          </div>
          <div className="watch-form-row">
            <label>
              Status
              <span>
                {u.isLockedOut
                  ? <span className="status-badge status-locked">Locked</span>
                  : <span className="status-badge status-active">Active</span>}
              </span>
            </label>
            <label>
              Failed Attempts
              <span className="user-card-value">{u.failedLoginAttempts}</span>
            </label>
          </div>
          <div className="user-card-actions">
            {u.isLockedOut && (
              <button className="btn btn-sm" onClick={() => handleUnlock(u.id)}>Unlock</button>
            )}
            <button className="btn btn-sm" onClick={() => setResetId(u.id)}>Reset Password</button>
          </div>
        </fieldset>
      ))}

      {resetId !== null && (
        <div className="modal-overlay" onClick={() => setResetId(null)}>
          <form className="modal" onClick={(e) => e.stopPropagation()} onSubmit={handleResetPassword}>
            <h3>Reset Password for {users.find((u) => u.id === resetId)?.username}</h3>
            <label>
              New Password
              <input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} required minLength={6} />
            </label>
            <div className="modal-actions">
              <button type="submit" className="btn">Reset</button>
              <button type="button" className="btn btn-danger" onClick={() => setResetId(null)}>Cancel</button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}

function SettingsPanel() {
  const [settings, setSettings] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [saved, setSaved] = useState(false);
  const [resettingKey, setResettingKey] = useState(false);
  const [newApiKey, setNewApiKey] = useState('');
  const [ollamaModels, setOllamaModels] = useState<string[]>([]);
  const [ollamaLoading, setOllamaLoading] = useState(false);
  const [ollamaError, setOllamaError] = useState('');
  const savedTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    return () => { if (savedTimerRef.current !== null) clearTimeout(savedTimerRef.current); };
  }, []);

  useEffect(() => {
    getSettings().then((s) => {
      setSettings(s);
      if (s['AiProvider'] === 'Ollama' && s['OllamaUrl']) {
        loadOllamaModels(s['OllamaUrl']);
      }
    }).finally(() => setLoading(false));
  }, []);

  async function loadOllamaModels(url: string) {
    setOllamaLoading(true);
    setOllamaError('');
    try {
      const models = await getOllamaModels(url);
      setOllamaModels(models);
    } catch {
      setOllamaError('Failed to connect to Ollama. Check the URL and try again.');
      setOllamaModels([]);
    } finally {
      setOllamaLoading(false);
    }
  }

  async function handleSave(e: FormEvent) {
    e.preventDefault();
    const entries = Object.entries(settings).map(([key, value]) => ({ key, value }));
    if (resettingKey && newApiKey) {
      entries.push({ key: 'AnthropicApiKey', value: newApiKey });
    }
    await updateSettings(entries);
    if (resettingKey && newApiKey) {
      setSettings({ ...settings, AnthropicApiKey: newApiKey.substring(0, 10) + '...' });
      setResettingKey(false);
      setNewApiKey('');
    }
    setSaved(true);
    savedTimerRef.current = setTimeout(() => setSaved(false), 2000);
  }

  if (loading) return <p>Loading…</p>;

  const provider = settings['AiProvider'] ?? 'Anthropic';
  const maskedKey = settings['AnthropicApiKey'] ?? '';

  return (
    <form className="settings-form" onSubmit={handleSave}>
      <fieldset className="watch-form-group">
        <legend>AI Configuration</legend>
        <label>
          AI Provider
          <select
            value={provider}
            onChange={(e) => {
              setSettings({ ...settings, AiProvider: e.target.value });
              if (e.target.value === 'Ollama' && settings['OllamaUrl']) {
                loadOllamaModels(settings['OllamaUrl']);
              }
            }}
          >
            <option value="Anthropic">Anthropic</option>
            <option value="Ollama">Ollama</option>
          </select>
        </label>

        {provider === 'Anthropic' && (
          <label>
            Anthropic API Key
            {!resettingKey ? (
              <div className="settings-inline-field">
                <input type="text" value={maskedKey} disabled />
                <button type="button" className="btn btn-sm" onClick={() => setResettingKey(true)}>Reset</button>
              </div>
            ) : (
              <div className="settings-inline-field">
                <input
                  type="password"
                  value={newApiKey}
                  onChange={(e) => setNewApiKey(e.target.value)}
                  placeholder="sk-ant-..."
                />
                <button type="button" className="btn btn-sm" onClick={() => { setResettingKey(false); setNewApiKey(''); }}>Cancel</button>
              </div>
            )}
          </label>
        )}

        {provider === 'Ollama' && (
          <>
            <label>
              Ollama URL
              <div className="settings-inline-field">
                <input
                  type="url"
                  value={settings['OllamaUrl'] ?? 'http://localhost:11434'}
                  onChange={(e) => setSettings({ ...settings, OllamaUrl: e.target.value })}
                  placeholder="http://localhost:11434"
                />
                <button
                  type="button"
                  className="btn btn-sm"
                  disabled={ollamaLoading}
                  onClick={() => loadOllamaModels(settings['OllamaUrl'] ?? 'http://localhost:11434')}
                >
                  {ollamaLoading ? 'Loading…' : 'Load Models'}
                </button>
              </div>
              {ollamaError && <small className="error">{ollamaError}</small>}
            </label>
            <label>
              Ollama Model
              {ollamaModels.length > 0 ? (
                <select
                  value={settings['OllamaModel'] ?? ''}
                  onChange={(e) => setSettings({ ...settings, OllamaModel: e.target.value })}
                >
                  <option value="">Select a model…</option>
                  {ollamaModels.map((m) => (
                    <option key={m} value={m}>{m}</option>
                  ))}
                </select>
              ) : (
                <input
                  type="text"
                  value={settings['OllamaModel'] ?? ''}
                  onChange={(e) => setSettings({ ...settings, OllamaModel: e.target.value })}
                  placeholder="e.g. llava, llava:13b"
                />
              )}
              <small>Use a vision-capable model (e.g. llava, llava:13b, bakllava).</small>
            </label>
          </>
        )}

        <label>
          AI Analysis Prompt
          <textarea
            rows={4}
            value={settings['AiAnalysisPrompt'] ?? ''}
            onChange={(e) => setSettings({ ...settings, AiAnalysisPrompt: e.target.value })}
          />
        </label>
      </fieldset>

      <fieldset className="watch-form-group">
        <legend>Security</legend>
        <div className="watch-form-row">
          <label>
            Max Failed Login Attempts
            <input
              type="number"
              min="1"
              value={settings['MaxFailedAttempts'] ?? '5'}
              onChange={(e) => setSettings({ ...settings, MaxFailedAttempts: e.target.value })}
            />
          </label>
          <label>
            Lockout Duration (minutes)
            <input
              type="number"
              min="1"
              value={settings['LockoutDurationMinutes'] ?? '15'}
              onChange={(e) => setSettings({ ...settings, LockoutDurationMinutes: e.target.value })}
            />
          </label>
        </div>
      </fieldset>

      <fieldset className="watch-form-group">
        <legend>Logging</legend>
        <label>
          Log Level
          <select
            value={settings['LogLevel'] ?? 'Information'}
            onChange={(e) => setSettings({ ...settings, LogLevel: e.target.value })}
          >
            <option value="Trace">Trace</option>
            <option value="Debug">Debug</option>
            <option value="Information">Information</option>
            <option value="Warning">Warning</option>
            <option value="Error">Error</option>
            <option value="Critical">Critical</option>
            <option value="None">None</option>
          </select>
        </label>
      </fieldset>

      <button type="submit" className="btn">Save Settings</button>
      {saved && <span className="save-success">✓ Saved</span>}
    </form>
  );
}
