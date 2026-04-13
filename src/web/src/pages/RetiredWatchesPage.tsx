import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getWatches, unretireWatch } from '../api/watches';
import { imageUrl } from '../api/watches';
import useIsPwa from '../hooks/useIsPwa';
import type { Watch } from '../types';

export default function RetiredWatchesPage() {
  const [watches, setWatches] = useState<Watch[]>([]);
  const [loading, setLoading] = useState(true);
  const isPwa = useIsPwa();

  useEffect(() => {
    loadRetired();
  }, []);

  async function loadRetired() {
    setLoading(true);
    try {
      const all = await getWatches(true);
      setWatches(all.filter((w) => w.isRetired));
    } finally {
      setLoading(false);
    }
  }

  async function handleUnretire(id: number) {
    await unretireWatch(id);
    setWatches((prev) => prev.filter((w) => w.id !== id));
  }

  if (loading) return <p>Loading…</p>;

  return (
    <div className="retired-watches-page">
      {!isPwa && <Link to="/settings" className="back-link">← Back to Settings</Link>}
      <h1>Retired Watches</h1>
      {watches.length === 0 ? (
        <p className="empty-state">No retired watches.</p>
      ) : (
        <div className="retired-watches-list">
          {watches.map((w) => (
            <div key={w.id} className="retired-watch-card">
              <div className="retired-watch-image">
                {w.imageUrls.length > 0 ? (
                  <img src={imageUrl(w.imageUrls[0].url)} alt={`${w.brand} ${w.model}`} />
                ) : (
                  <div className="retired-watch-placeholder">⌚</div>
                )}
              </div>
              <div className="retired-watch-info">
                <h3>{w.brand} {w.model}</h3>
                {w.retiredAt && (
                  <p className="retired-date">
                    Retired {new Date(w.retiredAt).toLocaleDateString()}
                  </p>
                )}
                <p className="retired-stats">
                  Worn {w.timesWorn} time{w.timesWorn !== 1 ? 's' : ''}
                  {w.lastWornDate && <> · Last worn {new Date(w.lastWornDate).toLocaleDateString()}</>}
                </p>
              </div>
              <button className="btn btn-sm" onClick={() => handleUnretire(w.id)}>
                Restore
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
