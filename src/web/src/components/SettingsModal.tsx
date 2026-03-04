import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { usePreferences } from '../context/PreferencesContext';
import { useAuth } from '../context/AuthContext';
import { changePassword, updateUsername as apiUpdateUsername, uploadProfileImage, deleteProfileImage } from '../api/auth';
import { gravatarUrl } from '../utils/gravatar';

interface SettingsModalProps {
  open: boolean;
  onClose: () => void;
}

export default function SettingsModal({ open, onClose }: SettingsModalProps) {
  const { theme, setTheme, timezone, setTimezone, defaultView, setDefaultView } = usePreferences();
  const { user, updateProfileImage, updateUsername } = useAuth();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const overlayRef = useRef<HTMLDivElement>(null);

  const [username, setUsername] = useState(user?.username ?? '');
  const [userSaved, setUserSaved] = useState(false);

  const [currentPw, setCurrentPw] = useState('');
  const [newPw, setNewPw] = useState('');
  const [confirmPw, setConfirmPw] = useState('');
  const [pwError, setPwError] = useState('');
  const [pwSuccess, setPwSuccess] = useState(false);
  const [pwLoading, setPwLoading] = useState(false);

  // Sync username when user changes or modal opens
  useEffect(() => {
    if (open) setUsername(user?.username ?? '');
  }, [open, user?.username]);

  // Close on Escape
  useEffect(() => {
    if (!open) return;
    const handleKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', handleKey);
    return () => window.removeEventListener('keydown', handleKey);
  }, [open, onClose]);

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

  async function handleSaveAccount(e: FormEvent) {
    e.preventDefault();
    if (username !== user?.username) {
      await apiUpdateUsername(username);
      updateUsername(username);
    }
    setUserSaved(true);
    setTimeout(() => setUserSaved(false), 2000);
  }

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

  if (!open) return null;

  return (
    <div
      className="modal-overlay"
      ref={overlayRef}
      onClick={(e) => { if (e.target === overlayRef.current) onClose(); }}
    >
      <div className="modal settings-modal">
        <div className="settings-modal-header">
          <h2>Settings</h2>
          <button type="button" className="settings-modal-close" onClick={onClose} aria-label="Close">✕</button>
        </div>

        <div className="settings-modal-body">
          <form className="settings-form" onSubmit={handleSaveAccount}>
            <fieldset className="watch-form-group">
              <legend>My Account</legend>
              {user && (
                <div className="settings-profile">
                  <div className="settings-avatar-wrapper">
                    <img
                      src={user.profileImage || gravatarUrl(user.email, 128)}
                      alt={user.username}
                      className="settings-avatar"
                    />
                    <input
                      ref={fileInputRef}
                      type="file"
                      accept="image/*"
                      hidden
                      onChange={async (e) => {
                        const file = e.target.files?.[0];
                        if (!file) return;
                        const url = await uploadProfileImage(file);
                        updateProfileImage(url);
                        e.target.value = '';
                      }}
                    />
                    <div className="settings-avatar-actions">
                      <button type="button" className="btn btn-sm" onClick={() => fileInputRef.current?.click()}>
                        Upload Photo
                      </button>
                      {user.profileImage && (
                        <button
                          type="button"
                          className="btn btn-danger btn-sm"
                          onClick={async () => {
                            await deleteProfileImage();
                            updateProfileImage(null);
                          }}
                        >
                          Remove
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              )}
              <div className="watch-form-row">
                <label>
                  Username
                  <input value={username} onChange={(e) => setUsername(e.target.value)} required />
                </label>
                <label>
                  Email
                  <input type="email" value={user?.email ?? ''} disabled />
                </label>
              </div>
              <button type="submit" className="btn">Save Account</button>
              {userSaved && <span className="save-success">✓ Saved</span>}
            </fieldset>
          </form>

          <form className="settings-form" onSubmit={handleChangePassword}>
            <fieldset className="watch-form-group">
              <legend>Change Password</legend>
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
            </fieldset>
          </form>

          <div className="settings-form">
            <fieldset className="watch-form-group">
              <legend>Appearance</legend>
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
              <div className="settings-row">
                <label htmlFor="view-select">Default View</label>
                <div className="theme-toggle">
                  <button
                    className={`theme-btn${defaultView === 'gallery' ? ' active' : ''}`}
                    onClick={() => setDefaultView('gallery')}
                    aria-pressed={defaultView === 'gallery'}
                    type="button"
                  >
                    ▦ Gallery
                  </button>
                  <button
                    className={`theme-btn${defaultView === 'table' ? ' active' : ''}`}
                    onClick={() => setDefaultView('table')}
                    aria-pressed={defaultView === 'table'}
                    type="button"
                  >
                    ☰ Table
                  </button>
                </div>
              </div>
            </fieldset>

            <fieldset className="watch-form-group">
              <legend>Regional</legend>
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
            </fieldset>
          </div>
        </div>
      </div>
    </div>
  );
}
