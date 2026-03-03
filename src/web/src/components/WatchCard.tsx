import { Link } from 'react-router-dom';
import type { Watch } from '../types';
import { imageUrl } from '../api/watches';

export default function WatchCard({ watch }: { watch: Watch }) {
  return (
    <Link to={`/watches/${watch.id}`} className="watch-card">
      {watch.imageUrls.length > 0 && (
        <img src={imageUrl(watch.imageUrls[0].url)} alt={`${watch.brand} ${watch.model}`} className="watch-card-image" />
      )}
      <h3>{watch.brand}</h3>
      <p className="watch-model">{watch.model}</p>
      <div className="watch-meta">
        <span>{watch.movementType}</span>
        {watch.caseSizeMm && <span>{watch.caseSizeMm}mm</span>}
      </div>
    </Link>
  );
}
