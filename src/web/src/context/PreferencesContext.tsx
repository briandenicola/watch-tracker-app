import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';

export type Theme = 'light' | 'dark';

interface PreferencesContextType {
  theme: Theme;
  setTheme: (theme: Theme) => void;
  timezone: string;
  setTimezone: (tz: string) => void;
}

const PreferencesContext = createContext<PreferencesContextType | null>(null);

const THEME_KEY = 'watch-tracker-theme';
const TZ_KEY = 'watch-tracker-timezone';

function getInitialTheme(): Theme {
  const stored = localStorage.getItem(THEME_KEY);
  if (stored === 'light' || stored === 'dark') return stored;
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

function getInitialTimezone(): string {
  return localStorage.getItem(TZ_KEY) || Intl.DateTimeFormat().resolvedOptions().timeZone;
}

export function PreferencesProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<Theme>(getInitialTheme);
  const [timezone, setTimezoneState] = useState<string>(getInitialTimezone);

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem(THEME_KEY, theme);
  }, [theme]);

  useEffect(() => {
    localStorage.setItem(TZ_KEY, timezone);
  }, [timezone]);

  const setTheme = (t: Theme) => setThemeState(t);
  const setTimezone = (tz: string) => setTimezoneState(tz);

  return (
    <PreferencesContext.Provider value={{ theme, setTheme, timezone, setTimezone }}>
      {children}
    </PreferencesContext.Provider>
  );
}

export function usePreferences() {
  const ctx = useContext(PreferencesContext);
  if (!ctx) throw new Error('usePreferences must be used within PreferencesProvider');
  return ctx;
}
