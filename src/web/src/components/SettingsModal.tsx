import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { usePreferences } from '../context/PreferencesContext';
import { useAuth } from '../context/AuthContext';
import { changePassword, updateUsername as apiUpdateUsername, uploadProfileImage, deleteProfileImage } from '../api/auth';
import { exportData, importData } from '../api/data';
import { getApiKeys, createApiKey, deleteApiKey, type ApiKeyDto, type ApiKeyCreatedDto } from '../api/apikeys';
import { AlertDialog } from './ConfirmDialog';
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

  const [exporting, setExporting] = useState(false);
  const [importing, setImporting] = useState(false);
  const [importResult, setImportResult] = useState<string | null>(null);
  const [importError, setImportError] = useState('');
  const importInputRef = useRef<HTMLInputElement>(null);

  const [apiKeys, setApiKeys] = useState<ApiKeyDto[]>([]);
  const [newKeyName, setNewKeyName] = useState('');
  const [createdKey, setCreatedKey] = useState<ApiKeyCreatedDto | null>(null);
  const [keyCopied, setKeyCopied] = useState(false);
  const [keyLoading, setKeyLoading] = useState(false);
  const [keyDeleting, setKeyDeleting] = useState<number | null>(null);
  const [alertMessage, setAlertMessage] = useState<string | null>(null);

  // Sync username when user changes or modal opens
  useEffect(() => {
    if (open) setUsername(user?.username ?? '');
  }, [open, user?.username]);

  // Load API keys when modal opens
  useEffect(() => {
    if (open) {
      getApiKeys().then(setApiKeys).catch(() => {});
    } else {
      setCreatedKey(null);
      setKeyCopied(false);
    }
  }, [open]);

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

  async function handleExport() {
    setExporting(true);
    try {
      const blob = await exportData();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'watch-collection-export.zip';
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      setAlertMessage('Export failed. Please try again.');
    } finally {
      setExporting(false);
    }
  }

  async function handleImport(file: File) {
    setImporting(true);
    setImportResult(null);
    setImportError('');
    try {
      const result = await importData(file);
      setImportResult(`Imported ${result.watchesImported} watches, ${result.imagesImported} images, ${result.wearLogsImported} wear logs.`);
    } catch {
      setImportError('Import failed. Make sure the file is a valid export ZIP.');
    } finally {
      setImporting(false);
    }
  }

  async function handleCreateApiKey(e: FormEvent) {
    e.preventDefault();
    if (!newKeyName.trim()) return;
    setKeyLoading(true);
    try {
      const result = await createApiKey(newKeyName.trim());
      setCreatedKey(result);
      setKeyCopied(false);
      setNewKeyName('');
      setApiKeys((prev) => [{ id: result.id, name: result.name, createdAt: result.createdAt, lastUsedAt: null }, ...prev]);
    } catch {
      setAlertMessage('Failed to create API key.');
    } finally {
      setKeyLoading(false);
    }
  }

  async function handleDeleteApiKey(id: number) {
    setKeyDeleting(id);
    try {
      await deleteApiKey(id);
      setApiKeys((prev) => prev.filter((k) => k.id !== id));
      if (createdKey?.id === id) setCreatedKey(null);
    } catch {
      setAlertMessage('Failed to delete API key.');
    } finally {
      setKeyDeleting(null);
    }
  }

  async function handleCopyKey(key: string) {
    await navigator.clipboard.writeText(key);
    setKeyCopied(true);
    setTimeout(() => setKeyCopied(false), 3000);
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

            <fieldset className="watch-form-group">
              <legend>Collection</legend>
              <p className="settings-hint">Manage watches that have been retired from your active collection.</p>
              <Link to="/retired" className="btn" onClick={onClose}>View Retired Watches</Link>
            </fieldset>

            <fieldset className="watch-form-group">
              <legend>Data Management</legend>
              <p className="settings-hint">Export your entire collection (watches, images, and wear history) as a ZIP file, or import from a previous export.</p>
              <div className="data-management-actions">
                <button type="button" className="btn" onClick={handleExport} disabled={exporting}>
                  {exporting ? 'Exporting…' : 'Export Collection'}
                </button>
                <button type="button" className="btn" onClick={() => importInputRef.current?.click()} disabled={importing}>
                  {importing ? 'Importing…' : 'Import Collection'}
                </button>
                <input
                  ref={importInputRef}
                  type="file"
                  accept=".zip"
                  hidden
                  onChange={(e) => {
                    const file = e.target.files?.[0];
                    if (file) handleImport(file);
                    e.target.value = '';
                  }}
                />
              </div>
              {importResult && <span className="save-success">✓ {importResult}</span>}
              {importError && <p className="error">{importError}</p>}
            </fieldset>

            <fieldset className="watch-form-group">
              <legend>API Keys</legend>
              <p className="settings-hint">
                Create API keys to access your collection from external applications. Use the <code>X-API-Key</code> header to authenticate requests.
              </p>

              <form className="api-key-create" onSubmit={handleCreateApiKey}>
                <input
                  type="text"
                  placeholder="Key name (e.g. Home Assistant)"
                  value={newKeyName}
                  onChange={(e) => setNewKeyName(e.target.value)}
                  required
                />
                <button type="submit" className="btn" disabled={keyLoading}>
                  {keyLoading ? 'Creating…' : 'Create Key'}
                </button>
              </form>

              {createdKey && (
                <div className="api-key-created-banner">
                  <p><strong>Save this key now — it won't be shown again:</strong></p>
                  <div className="api-key-value">
                    <code>{createdKey.key}</code>
                    <button type="button" className="btn btn-sm" onClick={() => handleCopyKey(createdKey.key)}>
                      {keyCopied ? '✓ Copied' : 'Copy'}
                    </button>
                  </div>
                </div>
              )}

              {apiKeys.length > 0 && (
                <table className="api-key-table">
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th>Created</th>
                      <th>Last Used</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {apiKeys.map((k) => (
                      <tr key={k.id}>
                        <td>{k.name}</td>
                        <td>{new Date(k.createdAt).toLocaleDateString()}</td>
                        <td>{k.lastUsedAt ? new Date(k.lastUsedAt).toLocaleDateString() : 'Never'}</td>
                        <td>
                          <button
                            type="button"
                            className="btn btn-danger btn-sm"
                            disabled={keyDeleting === k.id}
                            onClick={() => handleDeleteApiKey(k.id)}
                          >
                            {keyDeleting === k.id ? '…' : 'Delete'}
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
              {apiKeys.length === 0 && !createdKey && (
                <p className="settings-hint">No API keys yet.</p>
              )}
            </fieldset>
          </div>
        </div>
      </div>
      <AlertDialog
        open={alertMessage !== null}
        title="Error"
        message={alertMessage ?? ''}
        onClose={() => setAlertMessage(null)}
      />
    </div>
  );
}
