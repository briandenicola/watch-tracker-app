import { useMemo, useState, type FormEvent } from 'react';
import { usePreferences } from '../context/PreferencesContext';
import { useAuth } from '../context/AuthContext';
import { changePassword } from '../api/auth';
import { gravatarUrl } from '../utils/gravatar';

export default function SettingsPage() {
  const { theme, setTheme, timezone, setTimezone } = usePreferences();
  const { user } = useAuth();

  const [currentPw, setCurrentPw] = useState('');
  const [newPw, setNewPw] = useState('');
  const [confirmPw, setConfirmPw] = useState('');
  const [pwError, setPwError] = useState('');
  const [pwSuccess, setPwSuccess] = useState(false);
  const [pwLoading, setPwLoading] = useState(false);

  const timezones = useMemo(() => {
    try {
      return Intl.supportedValuesOf('timeZone');
    } catch {
      return [
        'UTC',
        'America/New_York', 'America/Chicago', 'America/Denver', 'America/Los_Angeles',
        'America/Anchorage', 'Pacific/Honolulu', 'America/Toronto', 'America/Vancouver',
        'Europe/London', 'Europe/Paris', 'Europe/Berlin', 'Europe/Rome', 'Europe/Madrid',
        'Europe/Zurich', 'Asia/Tokyo', 'Asia/Shanghai', 'Asia/Hong_Kong', 'Asia/Singapore',
        'Asia/Dubai', 'Asia/Kolkata', 'Australia/Sydney', 'Pacific/Auckland',
      ];
    }
  }, []);

  async function handleChangePassword(e: FormEvent) {
    e.preventDefault();
    setPwError('');
    setPwSuccess(false);

    if (newPw !== confirmPw) {
      setPwError('New passwords do not match.');
      return;
    }
    if (newPw.length < 6) {
      setPwError('New password must be at least 6 characters.');
      return;
    }

    setPwLoading(true);
    try {
      await changePassword(currentPw, newPw);
      setPwSuccess(true);
      setCurrentPw('');
      setNewPw('');
      setConfirmPw('');
      setTimeout(() => setPwSuccess(false), 3000);
    } catch {
      setPwError('Current password is incorrect.');
    } finally {
      setPwLoading(false);
    }
  }

  return (
    <div className="settings-page">
      <h1>Settings</h1>

      {user && (
        <section className="settings-section settings-profile">
          <img src={gravatarUrl(user.email, 128)} alt={user.username} className="settings-avatar" />
          <div>
            <p className="settings-profile-name">{user.username}</p>
            <p className="settings-profile-email">{user.email}</p>
          </div>
        </section>
      )}

      <section className="settings-section">
        <h2>Change Password</h2>
        <form className="settings-pw-form" onSubmit={handleChangePassword}>
          <label>
            Current Password
            <input type="password" value={currentPw} onChange={(e) => setCurrentPw(e.target.value)} required />
          </label>
          <label>
            New Password
            <input type="password" value={newPw} onChange={(e) => setNewPw(e.target.value)} required minLength={6} />
          </label>
          <label>
            Confirm New Password
            <input type="password" value={confirmPw} onChange={(e) => setConfirmPw(e.target.value)} required minLength={6} />
          </label>
          {pwError && <p className="error">{pwError}</p>}
          {pwSuccess && <span className="save-success">✓ Password changed</span>}
          <button type="submit" className="btn" disabled={pwLoading}>
            {pwLoading ? 'Changing…' : 'Change Password'}
          </button>
        </form>
      </section>

      <section className="settings-section">
        <h2>Appearance</h2>
        <div className="settings-row">
          <label htmlFor="theme-select">Theme</label>
          <div className="theme-toggle">
            <button
              className={`theme-btn${theme === 'light' ? ' active' : ''}`}
              onClick={() => setTheme('light')}
              aria-pressed={theme === 'light'}
              type="button"
            >
              ☀️ Light
            </button>
            <button
              className={`theme-btn${theme === 'dark' ? ' active' : ''}`}
              onClick={() => setTheme('dark')}
              aria-pressed={theme === 'dark'}
              type="button"
            >
              🌙 Dark
            </button>
          </div>
        </div>
      </section>

      <section className="settings-section">
        <h2>Regional</h2>
        <div className="settings-row">
          <label htmlFor="tz-select">Time Zone</label>
          <select
            id="tz-select"
            value={timezone}
            onChange={(e) => setTimezone(e.target.value)}
          >
            {timezones.map((tz) => (
              <option key={tz} value={tz}>{tz.replace(/_/g, ' ')}</option>
            ))}
          </select>
        </div>
        <p className="settings-hint">
          Current time: {new Date().toLocaleTimeString('en-US', { timeZone: timezone, timeZoneName: 'short' })}
        </p>
      </section>
    </div>
  );
}
