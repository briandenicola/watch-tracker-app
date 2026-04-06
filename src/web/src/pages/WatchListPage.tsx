import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { getWatches } from '../api/watches';
import WatchCard from '../components/WatchCard';
import SwipeGallery from '../components/SwipeGallery';
import PullToRefresh from '../components/PullToRefresh';
import { usePreferences } from '../context/PreferencesContext';
import { useAuth } from '../context/AuthContext';
import useIsPwa from '../hooks/useIsPwa';
import type { Watch } from '../types';

const PAGE_SIZE = 12;

type ViewMode = 'gallery' | 'table';
type SortOption = 'dateAdded' | 'brand' | 'lastWorn';
type TableSortField = 'brand' | 'model' | 'movementType' | 'caseSizeMm' | 'createdAt' | 'timesWorn' | 'lastWornDate';
type SortDir = 'asc' | 'desc';

function formatDate(value?: string | null) {
  if (!value) return '—';
  return new Date(value).toLocaleDateString();
}

export default function WatchListPage() {
  const { defaultView } = usePreferences();
  const { user, logout } = useAuth();
  const isPwa = useIsPwa();
  const [searchParams] = useSearchParams();
  const [watches, setWatches] = useState<Watch[]>([]);
  const [loading, setLoading] = useState(true);

  const [viewMode, setViewMode] = useState<ViewMode>(defaultView);
  const [brandFilter, setBrandFilter] = useState('');
  const [bandTypeFilter, setBandTypeFilter] = useState('');
  const [showWishList, setShowWishList] = useState(searchParams.has('wishlist'));

  useEffect(() => {
    setShowWishList(searchParams.has('wishlist'));
  }, [searchParams]);
  const [sort, setSort] = useState<SortOption>('dateAdded');
  const [gallerySortDir, setGallerySortDir] = useState<SortDir>('desc');
  const [visibleCount, setVisibleCount] = useState(PAGE_SIZE);

  const [tableSort, setTableSort] = useState<TableSortField>('createdAt');
  const [tableSortDir, setTableSortDir] = useState<SortDir>('desc');

  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  const sentinelRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  useEffect(() => {
    getWatches()
      .then(setWatches)
      .catch(() => setWatches([]))
      .finally(() => setLoading(false));
  }, []);

  const handleRefresh = useCallback(async () => {
    const fresh = await getWatches().catch(() => [] as Watch[]);
    setWatches(fresh);
  }, []);

  // Close popover on outside click
  useEffect(() => {
    if (!menuOpen) return;
    const handler = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setMenuOpen(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [menuOpen]);

  const handleBrandChange = (v: string) => { setBrandFilter(v); setVisibleCount(PAGE_SIZE); };
  const handleBandTypeChange = (v: string) => { setBandTypeFilter(v); setVisibleCount(PAGE_SIZE); };
  const handleSortChange = (v: SortOption) => { setSort(v); setVisibleCount(PAGE_SIZE); };

  const handleTableSort = (field: TableSortField) => {
    if (tableSort === field) {
      setTableSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      setTableSort(field);
      setTableSortDir('asc');
    }
  };

  const brands = useMemo(
    () => [...new Set(watches.map((w) => w.brand))].sort(),
    [watches],
  );

  const bandTypes = useMemo(
    () => [...new Set(watches.map((w) => w.bandType).filter(Boolean))].sort() as string[],
    [watches],
  );

  const baseFiltered = useMemo(() => {
    let result = watches.filter((w) => showWishList ? w.isWishList : !w.isWishList);
    if (brandFilter) result = result.filter((w) => w.brand === brandFilter);
    if (bandTypeFilter) result = result.filter((w) => w.bandType === bandTypeFilter);
    return result;
  }, [watches, brandFilter, bandTypeFilter, showWishList]);

  const galleryList = useMemo(() => {
    const result = [...baseFiltered];
    const dir = gallerySortDir === 'asc' ? 1 : -1;
    if (sort === 'brand') {
      result.sort((a, b) => dir * a.brand.localeCompare(b.brand));
    } else if (sort === 'lastWorn') {
      result.sort((a, b) => dir * (new Date(a.lastWornDate ?? 0).getTime() - new Date(b.lastWornDate ?? 0).getTime()));
    } else {
      result.sort((a, b) => dir * (new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()));
    }
    return result;
  }, [baseFiltered, sort, gallerySortDir]);

  const tableList = useMemo(() => {
    const result = [...baseFiltered];
    const dir = tableSortDir === 'asc' ? 1 : -1;
    result.sort((a, b) => {
      switch (tableSort) {
        case 'brand':
          return dir * a.brand.localeCompare(b.brand);
        case 'model':
          return dir * a.model.localeCompare(b.model);
        case 'movementType':
          return dir * a.movementType.localeCompare(b.movementType);
        case 'caseSizeMm':
          return dir * ((a.caseSizeMm ?? 0) - (b.caseSizeMm ?? 0));
        case 'timesWorn':
          return dir * (a.timesWorn - b.timesWorn);
        case 'lastWornDate':
          return dir * (new Date(a.lastWornDate ?? 0).getTime() - new Date(b.lastWornDate ?? 0).getTime());
        case 'createdAt':
        default:
          return dir * (new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
      }
    });
    return result;
  }, [baseFiltered, tableSort, tableSortDir]);

  const visible = galleryList.slice(0, visibleCount);
  const hasMore = visibleCount < galleryList.length;

  const loadMore = useCallback(() => {
    setVisibleCount((c) => Math.min(c + PAGE_SIZE, galleryList.length));
  }, [galleryList.length]);

  useEffect(() => {
    const el = sentinelRef.current;
    if (!el) return;
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting) loadMore();
      },
      { rootMargin: '200px' },
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, [loadMore]);

  const sortIndicator = (field: TableSortField) =>
    tableSort === field ? (tableSortDir === 'asc' ? ' ▲' : ' ▼') : '';

  const isWishList = location.search.includes('wishlist');
  const title = isWishList
    ? 'Watch Tracker — Wish List'
    : (user?.username ? `${user.username.charAt(0).toUpperCase() + user.username.slice(1)}'s Watches` : 'My Watches');

  if (loading) return <p>Loading…</p>;

  // PWA mode: compact header with hamburger menu
  if (isPwa) {
    return (
      <div className="watch-list-page">
        <div className="pwa-menu-wrapper pwa-menu-titlebar" ref={menuRef}>
          <button
            className="pwa-hamburger"
            onClick={() => setMenuOpen((v) => !v)}
            aria-label="Menu"
          >
            <span /><span /><span />
          </button>
          {menuOpen && (
            <div className="pwa-popover">
              <div className="pwa-popover-section">
                <label className="pwa-popover-label">View</label>
                <div className="view-toggle">
                  <button
                    className={`view-toggle-btn${viewMode === 'gallery' ? ' active' : ''}`}
                    onClick={() => { setViewMode('gallery'); }}
                  >▦ Gallery</button>
                  <button
                    className={`view-toggle-btn${viewMode === 'table' ? ' active' : ''}`}
                    onClick={() => { setViewMode('table'); }}
                  >☰ Table</button>
                </div>
              </div>

              <div className="pwa-popover-section">
                <label className="pwa-popover-label">Filters</label>
                <select value={brandFilter} onChange={(e) => handleBrandChange(e.target.value)}>
                  <option value="">All Brands</option>
                  {brands.map((b) => <option key={b} value={b}>{b}</option>)}
                </select>
                <select value={bandTypeFilter} onChange={(e) => handleBandTypeChange(e.target.value)}>
                  <option value="">All Band Types</option>
                  {bandTypes.map((bt) => <option key={bt} value={bt}>{bt}</option>)}
                </select>
              </div>

              {viewMode === 'gallery' && (
                <div className="pwa-popover-section">
                  <label className="pwa-popover-label">Sort</label>
                  <div className="pwa-sort-row">
                    <select value={sort} onChange={(e) => handleSortChange(e.target.value as SortOption)}>
                      <option value="dateAdded">Date Added</option>
                      <option value="brand">Brand</option>
                      <option value="lastWorn">Last Worn</option>
                    </select>
                    <button
                      className="btn-sort-dir"
                      onClick={() => { setGallerySortDir((d) => (d === 'asc' ? 'desc' : 'asc')); setVisibleCount(PAGE_SIZE); }}
                    >{gallerySortDir === 'asc' ? '▲' : '▼'}</button>
                  </div>
                </div>
              )}

              <div className="pwa-popover-section">
                <button className="btn pwa-popover-btn pwa-logout-btn" onClick={() => { setMenuOpen(false); logout(); }}>
                  Log Out
                </button>
              </div>
            </div>
          )}
        </div>

        <PullToRefresh onRefresh={handleRefresh}>
          {baseFiltered.length === 0 ? (
            <p>{watches.length === 0 ? 'No watches yet. Add your first one!' : 'No watches match the selected filters.'}</p>
          ) : viewMode === 'gallery' ? (
            <SwipeGallery
              initialIndex={Number(sessionStorage.getItem(isWishList ? 'wishGalleryIdx' : 'galleryIdx') || '0')}
              onIndexChange={(i) => sessionStorage.setItem(isWishList ? 'wishGalleryIdx' : 'galleryIdx', String(i))}
            >
              {galleryList.map((w) => (
                <WatchCard key={w.id} watch={w} />
              ))}
            </SwipeGallery>
          ) : (
            <div className="watch-table-wrap">
              <table className="watch-table">
                <thead>
                  <tr>
                    <th onClick={() => handleTableSort('brand')}>Brand{sortIndicator('brand')}</th>
                    <th onClick={() => handleTableSort('model')}>Model{sortIndicator('model')}</th>
                    <th onClick={() => handleTableSort('createdAt')}>Added{sortIndicator('createdAt')}</th>
                    <th onClick={() => handleTableSort('timesWorn')}>Worn{sortIndicator('timesWorn')}</th>
                  </tr>
                </thead>
                <tbody>
                  {tableList.map((w) => (
                    <tr key={w.id} onClick={() => navigate(w.isWishList ? `/wishlist/${w.id}/edit` : `/watches/${w.id}`)} className="watch-table-row">
                      <td>{w.brand}</td>
                      <td>{w.model}</td>
                      <td>{formatDate(w.createdAt)}</td>
                      <td>{w.timesWorn}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </PullToRefresh>
      </div>
    );
  }

  // Standard browser mode
  return (
    <div className="watch-list-page">
      <div className="page-header">
        <h1>{title}</h1>
        <div className="page-header-actions">
          <Link to="/wishlist/new" className="btn">Add to Wish List</Link>
          <Link to="/watches/new" className="btn">Add Watch</Link>
        </div>
      </div>

      {watches.length > 0 && (
        <div className="watch-toolbar">
          <div className="view-toggle">
            <button
              className={`view-toggle-btn${viewMode === 'gallery' ? ' active' : ''}`}
              onClick={() => setViewMode('gallery')}
              title="Gallery view"
            >▦</button>
            <button
              className={`view-toggle-btn${viewMode === 'table' ? ' active' : ''}`}
              onClick={() => setViewMode('table')}
              title="Table view"
            >☰</button>
          </div>

          <select value={brandFilter} onChange={(e) => handleBrandChange(e.target.value)}>
            <option value="">All Brands</option>
            {brands.map((b) => (
              <option key={b} value={b}>{b}</option>
            ))}
          </select>

          <select value={bandTypeFilter} onChange={(e) => handleBandTypeChange(e.target.value)}>
            <option value="">All Band Types</option>
            {bandTypes.map((bt) => (
              <option key={bt} value={bt}>{bt}</option>
            ))}
          </select>

          {viewMode === 'gallery' && (
            <>
              <select value={sort} onChange={(e) => handleSortChange(e.target.value as SortOption)}>
                <option value="dateAdded">Sort: Date Added</option>
                <option value="brand">Sort: Brand</option>
                <option value="lastWorn">Sort: Last Worn</option>
              </select>
              <button
                className="btn-sort-dir"
                onClick={() => { setGallerySortDir((d) => (d === 'asc' ? 'desc' : 'asc')); setVisibleCount(PAGE_SIZE); }}
                title={gallerySortDir === 'asc' ? 'Ascending' : 'Descending'}
              >{gallerySortDir === 'asc' ? '▲' : '▼'}</button>
            </>
          )}

          <button
            className={`btn btn-sm${showWishList ? ' active' : ''}`}
            onClick={() => { setShowWishList((v) => !v); setVisibleCount(PAGE_SIZE); }}
            type="button"
          >
            {showWishList ? 'Wish List' : 'Wish List'}
          </button>
        </div>
      )}

      {baseFiltered.length === 0 ? (
        <p>{watches.length === 0 ? 'No watches yet. Add your first one!' : 'No watches match the selected filters.'}</p>
      ) : viewMode === 'gallery' ? (
        <>
          <div className="watch-grid">
            {visible.map((w) => (
              <WatchCard key={w.id} watch={w} />
            ))}
          </div>
          {hasMore && <div ref={sentinelRef} className="scroll-sentinel" />}
        </>
      ) : (
        <div className="watch-table-wrap">
          <table className="watch-table">
            <thead>
              <tr>
                <th onClick={() => handleTableSort('brand')}>Brand{sortIndicator('brand')}</th>
                <th onClick={() => handleTableSort('model')}>Model{sortIndicator('model')}</th>
                <th className="hide-mobile" onClick={() => handleTableSort('caseSizeMm')}>Size{sortIndicator('caseSizeMm')}</th>
                <th className="hide-mobile" onClick={() => handleTableSort('movementType')}>Type{sortIndicator('movementType')}</th>
                <th onClick={() => handleTableSort('createdAt')}>Date Added{sortIndicator('createdAt')}</th>
                <th className="hide-mobile" onClick={() => handleTableSort('lastWornDate')}>Last Worn{sortIndicator('lastWornDate')}</th>
                <th className="hide-mobile" onClick={() => handleTableSort('timesWorn')}>Worn{sortIndicator('timesWorn')}</th>
              </tr>
            </thead>
            <tbody>
              {tableList.map((w) => (
                <tr key={w.id} onClick={() => navigate(w.isWishList ? `/wishlist/${w.id}/edit` : `/watches/${w.id}`)} className="watch-table-row">
                  <td>{w.brand}</td>
                  <td>{w.model}</td>
                  <td className="hide-mobile">{w.caseSizeMm ? `${w.caseSizeMm}mm` : '—'}</td>
                  <td className="hide-mobile">{w.movementType}</td>
                  <td>{formatDate(w.createdAt)}</td>
                  <td className="hide-mobile">{formatDate(w.lastWornDate)}</td>
                  <td className="hide-mobile">{w.timesWorn}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
