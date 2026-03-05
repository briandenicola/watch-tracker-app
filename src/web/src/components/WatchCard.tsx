import { Link } from 'react-router-dom';
import type { Watch } from '../types';
import { imageUrl } from '../api/watches';

export default function WatchCard({ watch }: { watch: Watch }) {
  const linkTo = watch.isWishList ? `/wishlist/${watch.id}/edit` : `/watches/${watch.id}`;
  return (
    <Link to={linkTo} className="watch-card">
      {watch.imageUrls.length > 0 && (
        <img src={imageUrl(watch.imageUrls[0].url)} alt={`${watch.brand} ${watch.model}`} className="watch-card-image" />
      )}
      <p className="watch-card-title">{watch.brand} {watch.model}</p>
    </Link>
  );
}
