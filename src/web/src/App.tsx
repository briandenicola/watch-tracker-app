import { BrowserRouter, Routes, Route, Link, Navigate } from 'react-router-dom';
import { useState } from 'react';
import ProtectedRoute from './components/ProtectedRoute';
import AdminProtectedRoute from './components/AdminProtectedRoute';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import SetupPage from './pages/SetupPage';
import WatchListPage from './pages/WatchListPage';
import WatchDetailPage from './pages/WatchDetailPage';
import AddWatchPage from './pages/AddWatchPage';
import EditWatchPage from './pages/EditWatchPage';
import AdminPage from './pages/AdminPage';
import SettingsPage from './pages/SettingsPage';
import { useAuth } from './context/AuthContext';
import { gravatarUrl } from './utils/gravatar';
import './App.css';

function App() {
  const { user, isAdmin, needsSetup, logout } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);

  if (needsSetup === null) return <p>Loading…</p>;

  return (
    <BrowserRouter>
      {user && !needsSetup && (
        <header className="app-header">
          <div className="app-header-left">
            <Link to="/" className="app-title">⌚ Watch Tracker</Link>
            <button className="hamburger" onClick={() => setMenuOpen((o) => !o)} aria-label="Menu">
              <span /><span /><span />
            </button>
          </div>
          <nav className={menuOpen ? 'nav-open' : ''}>
            {isAdmin && <Link to="/admin" className="nav-link" onClick={() => setMenuOpen(false)}>Admin</Link>}
            <Link to="/settings" className="nav-avatar" title={user.username} onClick={() => setMenuOpen(false)}>
              <img src={user.profileImage || gravatarUrl(user.email, 128)} alt={user.username} className="avatar" />
            </Link>
            <button onClick={() => { setMenuOpen(false); logout(); }}>Logout</button>
          </nav>
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
                <Route path="/settings" element={<SettingsPage />} />
              </Route>
              <Route element={<AdminProtectedRoute />}>
                <Route path="/admin" element={<AdminPage />} />
              </Route>
            </>
          )}
        </Routes>
      </main>
    </BrowserRouter>
  );
}

export default App;
