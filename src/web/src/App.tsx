import { BrowserRouter, Routes, Route, Link, Navigate } from 'react-router-dom';
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
import { useAuth } from './context/AuthContext';
import './App.css';

function App() {
  const { user, isAdmin, needsSetup, logout } = useAuth();

  if (needsSetup === null) return <p>Loading…</p>;

  return (
    <BrowserRouter>
      {user && !needsSetup && (
        <header className="app-header">
          <span>⌚ Watch Tracker</span>
          <nav>
            {isAdmin && <Link to="/admin" className="nav-link">Admin</Link>}
            <span>{user.username}</span>
            <button onClick={logout}>Logout</button>
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
