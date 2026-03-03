import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getWatches } from '../api/watches';
import WatchCard from '../components/WatchCard';
import type { Watch } from '../types';

export default function WatchListPage() {
  const [watches, setWatches] = useState<Watch[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getWatches()
      .then(setWatches)
      .catch(() => setWatches([]))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <p>Loading…</p>;

  return (
    <div className="watch-list-page">
      <div className="page-header">
        <h1>My Watches</h1>
        <Link to="/watches/new" className="btn">Add Watch</Link>
      </div>
      {watches.length === 0 ? (
        <p>No watches yet. Add your first one!</p>
      ) : (
        <div className="watch-grid">
          {watches.map((w) => (
            <WatchCard key={w.id} watch={w} />
          ))}
        </div>
      )}
    </div>
  );
}
