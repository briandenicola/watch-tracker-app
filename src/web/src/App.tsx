import { BrowserRouter, Routes, Route, Link, Navigate } from 'react-router-dom';
import { useState } from 'react';
import ProtectedRoute from './components/ProtectedRoute';
import AdminProtectedRoute from './components/AdminProtectedRoute';
import PwaBottomBar from './components/PwaBottomBar';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import SetupPage from './pages/SetupPage';
import WatchListPage from './pages/WatchListPage';
import WatchDetailPage from './pages/WatchDetailPage';
import AddWatchPage from './pages/AddWatchPage';
import EditWatchPage from './pages/EditWatchPage';
import AdminPage from './pages/AdminPage';
import StatsPage from './pages/StatsPage';
import SettingsPage from './pages/SettingsPage';
import AddWishListPage from './pages/AddWishListPage';
import EditWishListPage from './pages/EditWishListPage';
import SettingsModal from './components/SettingsModal';
import RetiredWatchesPage from './pages/RetiredWatchesPage';
import { useAuth } from './context/AuthContext';
import { gravatarUrl } from './utils/gravatar';
import useIsPwa from './hooks/useIsPwa';
import './App.css';

function App() {
  const { user, isAdmin, needsSetup, logout } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);
  const [settingsOpen, setSettingsOpen] = useState(false);
  const isPwa = useIsPwa();

  if (needsSetup === null) return <p>Loading…</p>;

  return (
    <BrowserRouter>
      <div className={isPwa ? 'pwa-shell' : undefined}>
      {user && !needsSetup && (
        <header className={`app-header${isPwa ? ' pwa-app-header' : ''}`}>
          <div className="app-header-left">
            {isPwa && (
              <img
                src={user.profileImage || gravatarUrl(user.email, 64)}
                alt={user.username}
                className="pwa-title-avatar"
              />
            )}
            <Link to="/" className="app-title">⌚ Watch Tracker</Link>
            {!isPwa && (
              <button className="hamburger" onClick={() => setMenuOpen((o) => !o)} aria-label="Menu">
                <span /><span /><span />
              </button>
            )}
          </div>
          {!isPwa && (
            <nav className={menuOpen ? 'nav-open' : ''}>
              {isAdmin && <Link to="/admin" className="nav-link" onClick={() => setMenuOpen(false)}>Admin</Link>}
              <Link to="/stats" className="nav-link" onClick={() => setMenuOpen(false)}>Stats</Link>
              <Link to="/?wishlist" className="nav-link" onClick={() => setMenuOpen(false)}>Wish List</Link>
              <Link to="/retired" className="nav-link" onClick={() => setMenuOpen(false)}>Retired</Link>
              <button className="nav-avatar" title={user.username} onClick={() => { setMenuOpen(false); setSettingsOpen(true); }}>
                <img src={user.profileImage || gravatarUrl(user.email, 128)} alt={user.username} className="avatar" />
              </button>
              <button onClick={() => { setMenuOpen(false); logout(); }}>Logout</button>
            </nav>
          )}
        </header>
      )}
      <main className="container">
        <Routes>
          {needsSetup ? (
            <>
              <Route path="/setup" element={<SetupPage />} />
              <Route path="*" element={<Navigate to="/setup" replace />} />
            </>
          ) : (
            <>
              <Route path="/login" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route path="/setup" element={<Navigate to="/" replace />} />
              <Route element={<ProtectedRoute />}>
                <Route path="/" element={<WatchListPage />} />
                <Route path="/watches/new" element={<AddWatchPage />} />
                <Route path="/watches/:id/edit" element={<EditWatchPage />} />
                <Route path="/watches/:id" element={<WatchDetailPage />} />
                <Route path="/stats" element={<StatsPage />} />
                <Route path="/settings" element={<SettingsPage />} />
                <Route path="/retired" element={<RetiredWatchesPage />} />
                <Route path="/wishlist/new" element={<AddWishListPage />} />
                <Route path="/wishlist/:id/edit" element={<EditWishListPage />} />
              </Route>
              <Route element={<AdminProtectedRoute />}>
                <Route path="/admin" element={<AdminPage />} />
              </Route>
            </>
          )}
        </Routes>
      </main>
      {user && !isPwa && <SettingsModal open={settingsOpen} onClose={() => setSettingsOpen(false)} />}
      {user && !needsSetup && isPwa && <PwaBottomBar />}
      </div>
    </BrowserRouter>
  );
}

export default App;
