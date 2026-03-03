import { useEffect, useState, type FormEvent } from 'react';
import { getUsers, unlockUser, resetUserPassword, getSettings, updateSettings } from '../api/admin';
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

  useEffect(() => {
    getSettings().then(setSettings).finally(() => setLoading(false));
  }, []);

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
    setTimeout(() => setSaved(false), 2000);
  }

  if (loading) return <p>Loading…</p>;

  const maskedKey = settings['AnthropicApiKey'] ?? '';

  return (
    <form className="settings-form" onSubmit={handleSave}>
      <fieldset className="watch-form-group">
        <legend>AI Configuration</legend>
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
