import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function PwaBottomBar() {
  const { isAdmin } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const path = location.pathname;
  const isWishList = path === '/' && location.search.includes('wishlist');

  return (
    <nav className="pwa-bottom-bar">
      <button
        className={`pwa-tab-item${isWishList ? ' active' : ''}`}
        onClick={() => navigate('/?wishlist')}
        aria-label="Wish List"
      >
        <svg width="24" height="24" viewBox="0 0 24 24" fill={isWishList ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2" />
        </svg>
      </button>
      <button
        className={`pwa-tab-item${path === '/stats' ? ' active' : ''}`}
        onClick={() => navigate('/stats')}
        aria-label="Stats"
      >
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
          <line x1="6" y1="20" x2="6" y2="14" />
          <line x1="12" y1="20" x2="12" y2="4" />
          <line x1="18" y1="20" x2="18" y2="10" />
        </svg>
      </button>
      <div className="pwa-tab-add-wrapper">
        <button
          className="pwa-tab-add"
          onClick={() => navigate(isWishList ? '/wishlist/new' : '/watches/new')}
          aria-label={isWishList ? 'Add to Wish List' : 'Add Watch'}
        >
          <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
        </button>
      </div>
      <button
        className={`pwa-tab-item${path === '/settings' ? ' active' : ''}`}
        onClick={() => navigate('/settings')}
        aria-label="Settings"
      >
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <circle cx="12" cy="12" r="3" />
          <path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42" />
        </svg>
      </button>
      {isAdmin && (
        <button
          className={`pwa-tab-item${path === '/admin' ? ' active' : ''}`}
          onClick={() => navigate('/admin')}
          aria-label="Admin"
        >
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
          </svg>
        </button>
      )}
    </nav>
  );
}
