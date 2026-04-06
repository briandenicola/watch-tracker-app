import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';

export type Theme = 'light' | 'dark';
export type DefaultView = 'gallery' | 'table';
export type DefaultLanding = 'watches' | 'wishlist';
export type DefaultSort = 'dateAdded' | 'brand' | 'lastWorn';
export type DefaultSortDir = 'asc' | 'desc';

interface PreferencesContextType {
  theme: Theme;
  setTheme: (theme: Theme) => void;
  timezone: string;
  setTimezone: (tz: string) => void;
  defaultView: DefaultView;
  setDefaultView: (view: DefaultView) => void;
  defaultLanding: DefaultLanding;
  setDefaultLanding: (landing: DefaultLanding) => void;
  defaultSort: DefaultSort;
  setDefaultSort: (sort: DefaultSort) => void;
  defaultSortDir: DefaultSortDir;
  setDefaultSortDir: (dir: DefaultSortDir) => void;
}

const PreferencesContext = createContext<PreferencesContextType | null>(null);

const THEME_KEY = 'watch-tracker-theme';
const TZ_KEY = 'watch-tracker-timezone';
const VIEW_KEY = 'watch-tracker-default-view';
const LANDING_KEY = 'watch-tracker-default-landing';
const SORT_KEY = 'watch-tracker-default-sort';
const SORT_DIR_KEY = 'watch-tracker-default-sort-dir';

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

function getInitialLanding(): DefaultLanding {
  const stored = localStorage.getItem(LANDING_KEY);
  if (stored === 'watches' || stored === 'wishlist') return stored;
  return 'watches';
}

function getInitialSort(): DefaultSort {
  const stored = localStorage.getItem(SORT_KEY);
  if (stored === 'dateAdded' || stored === 'brand' || stored === 'lastWorn') return stored;
  return 'dateAdded';
}

function getInitialSortDir(): DefaultSortDir {
  const stored = localStorage.getItem(SORT_DIR_KEY);
  if (stored === 'asc' || stored === 'desc') return stored;
  return 'desc';
}

export function PreferencesProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<Theme>(getInitialTheme);
  const [timezone, setTimezoneState] = useState<string>(getInitialTimezone);
  const [defaultView, setDefaultViewState] = useState<DefaultView>(getInitialView);
  const [defaultLanding, setDefaultLandingState] = useState<DefaultLanding>(getInitialLanding);
  const [defaultSort, setDefaultSortState] = useState<DefaultSort>(getInitialSort);
  const [defaultSortDir, setDefaultSortDirState] = useState<DefaultSortDir>(getInitialSortDir);

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

  useEffect(() => {
    localStorage.setItem(LANDING_KEY, defaultLanding);
  }, [defaultLanding]);

  useEffect(() => {
    localStorage.setItem(SORT_KEY, defaultSort);
  }, [defaultSort]);

  useEffect(() => {
    localStorage.setItem(SORT_DIR_KEY, defaultSortDir);
  }, [defaultSortDir]);

  const setTheme = (t: Theme) => setThemeState(t);
  const setTimezone = (tz: string) => setTimezoneState(tz);
  const setDefaultView = (v: DefaultView) => setDefaultViewState(v);
  const setDefaultLanding = (l: DefaultLanding) => setDefaultLandingState(l);
  const setDefaultSort = (s: DefaultSort) => setDefaultSortState(s);
  const setDefaultSortDir = (d: DefaultSortDir) => setDefaultSortDirState(d);

  return (
    <PreferencesContext.Provider value={{ theme, setTheme, timezone, setTimezone, defaultView, setDefaultView, defaultLanding, setDefaultLanding, defaultSort, setDefaultSort, defaultSortDir, setDefaultSortDir }}>
      {children}
    </PreferencesContext.Provider>
  );
}

export function usePreferences() {
  const ctx = useContext(PreferencesContext);
  if (!ctx) throw new Error('usePreferences must be used within PreferencesProvider');
  return ctx;
}
