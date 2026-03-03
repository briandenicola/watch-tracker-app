import { useMemo } from 'react';
import { usePreferences } from '../context/PreferencesContext';

export default function SettingsPage() {
  const { theme, setTheme, timezone, setTimezone } = usePreferences();

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

  return (
    <div className="settings-page">
      <h1>Settings</h1>

      <section className="settings-section">
        <h2>Appearance</h2>
        <div className="settings-row">
          <label htmlFor="theme-select">Theme</label>
          <div className="theme-toggle">
            <button
              className={`theme-btn${theme === 'light' ? ' active' : ''}`}
              onClick={() => setTheme('light')}
              aria-pressed={theme === 'light'}
            >
              ☀️ Light
            </button>
            <button
              className={`theme-btn${theme === 'dark' ? ' active' : ''}`}
              onClick={() => setTheme('dark')}
              aria-pressed={theme === 'dark'}
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
