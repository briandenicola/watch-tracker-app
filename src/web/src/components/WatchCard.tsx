import { Link } from 'react-router-dom';
import type { Watch } from '../types';
import { imageUrl } from '../api/watches';

export default function WatchCard({ watch }: { watch: Watch }) {
  if (watch.isWishList) {
    return (
      <div className="watch-card wishlist-card">
        <Link to={`/wishlist/${watch.id}/edit`}>
          {watch.imageUrls.length > 0 && (
            <div className="watch-card-image-wrap">
              <img src={imageUrl(watch.imageUrls[0].url)} alt={`${watch.brand} ${watch.model}`} className="watch-card-image" />
            </div>
          )}
        </Link>
        {watch.linkUrl ? (
          <a
            href={watch.linkUrl}
            className="watch-card-title wishlist-link"
            target="_blank"
            rel="noopener noreferrer"
            onClick={(e) => e.stopPropagation()}
          >
            {watch.brand} {watch.model}
          </a>
        ) : (
          <Link to={`/wishlist/${watch.id}/edit`} className="watch-card-title">
            {watch.brand} {watch.model}
          </Link>
        )}
        {watch.purchasePrice != null && (
          <p className="wishlist-price">${watch.purchasePrice.toFixed(2)}</p>
        )}
      </div>
    );
  }

  return (
    <Link to={`/watches/${watch.id}`} className="watch-card">
      {watch.imageUrls.length > 0 && (
        <div className="watch-card-image-wrap">
          <img src={imageUrl(watch.imageUrls[0].url)} alt={`${watch.brand} ${watch.model}`} className="watch-card-image" />
        </div>
      )}
      <p className="watch-card-title">{watch.brand} {watch.model}</p>
    </Link>
  );
}
