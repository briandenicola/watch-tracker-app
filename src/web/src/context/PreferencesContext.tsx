import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';

export type Theme = 'light' | 'dark';
export type DefaultView = 'gallery' | 'table';

interface PreferencesContextType {
  theme: Theme;
  setTheme: (theme: Theme) => void;
  timezone: string;
  setTimezone: (tz: string) => void;
  defaultView: DefaultView;
  setDefaultView: (view: DefaultView) => void;
}

const PreferencesContext = createContext<PreferencesContextType | null>(null);

const THEME_KEY = 'watch-tracker-theme';
const TZ_KEY = 'watch-tracker-timezone';
const VIEW_KEY = 'watch-tracker-default-view';

function getInitialTheme(): Theme {
  const stored = localStorage.getItem(THEME_KEY);
  if (stored === 'light' || stored === 'dark') return stored;
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

function getInitialTimezone(): string {
  return localStorage.getItem(TZ_KEY) || Intl.DateTimeFormat().resolvedOptions().timeZone;
}

function getInitialView(): DefaultView {
  const stored = localStorage.getItem(VIEW_KEY);
  if (stored === 'gallery' || stored === 'table') return stored;
  return 'gallery';
}

export function PreferencesProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<Theme>(getInitialTheme);
  const [timezone, setTimezoneState] = useState<string>(getInitialTimezone);
  const [defaultView, setDefaultViewState] = useState<DefaultView>(getInitialView);

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem(THEME_KEY, theme);
  }, [theme]);

  useEffect(() => {
    localStorage.setItem(TZ_KEY, timezone);
  }, [timezone]);

  useEffect(() => {
    localStorage.setItem(VIEW_KEY, defaultView);
  }, [defaultView]);

  const setTheme = (t: Theme) => setThemeState(t);
  const setTimezone = (tz: string) => setTimezoneState(tz);
  const setDefaultView = (v: DefaultView) => setDefaultViewState(v);

  return (
    <PreferencesContext.Provider value={{ theme, setTheme, timezone, setTimezone, defaultView, setDefaultView }}>
      {children}
    </PreferencesContext.Provider>
  );
}

export function usePreferences() {
  const ctx = useContext(PreferencesContext);
  if (!ctx) throw new Error('usePreferences must be used within PreferencesProvider');
  return ctx;
}
