import { Link } from 'react-router-dom';
import type { Watch } from '../types';
import { imageUrl } from '../api/watches';

export default function WatchCard({ watch }: { watch: Watch }) {
  if (watch.isWishList) {
    return (
      <Link to={`/wishlist/${watch.id}/edit`} className="watch-card wishlist-card">
        {watch.imageUrls.length > 0 && (
          <img src={imageUrl(watch.imageUrls[0].url)} alt={`${watch.brand} ${watch.model}`} className="watch-card-image" />
        )}
        {watch.linkUrl ? (
          <span
            className="watch-card-title wishlist-link"
            onClick={(e) => { e.preventDefault(); window.open(watch.linkUrl!, '_blank', 'noopener,noreferrer'); }}
          >
            {watch.brand} {watch.model}
          </span>
        ) : (
          <p className="watch-card-title">{watch.brand} {watch.model}</p>
        )}
        {watch.purchasePrice != null && (
          <p className="wishlist-price">${watch.purchasePrice.toFixed(2)}</p>
        )}
      </Link>
    );
  }

  return (
    <Link to={`/watches/${watch.id}`} className="watch-card">
      {watch.imageUrls.length > 0 && (
        <img src={imageUrl(watch.imageUrls[0].url)} alt={`${watch.brand} ${watch.model}`} className="watch-card-image" />
      )}
      <p className="watch-card-title">{watch.brand} {watch.model}</p>
    </Link>
  );
}
