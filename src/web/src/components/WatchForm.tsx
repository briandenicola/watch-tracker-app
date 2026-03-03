import { useState, type FormEvent, type ChangeEvent } from 'react';
import type { CreateWatch, MovementType } from '../types';

interface WatchFormProps {
  initial?: Partial<CreateWatch>;
  onSubmit: (data: CreateWatch, files: File[]) => void;
  submitLabel?: string;
  onCancel?: () => void;
}

const MOVEMENT_TYPES: MovementType[] = ['Automatic', 'Manual', 'Quartz', 'Digital'];
const BAND_TYPE_OPTIONS = ['Black Leather', 'Brown Leather', 'Titanium'];

export default function WatchForm({ initial, onSubmit, submitLabel = 'Save', onCancel }: WatchFormProps) {
  const [brand, setBrand] = useState(initial?.brand ?? '');
  const [model, setModel] = useState(initial?.model ?? '');
  const [movementType, setMovementType] = useState<MovementType>(initial?.movementType ?? 'Automatic');
  const [caseSizeMm, setCaseSizeMm] = useState(initial?.caseSizeMm?.toString() ?? '');

  const initialBand = initial?.bandType ?? '';
  const isPreset = BAND_TYPE_OPTIONS.includes(initialBand);
  const [bandTypeSelect, setBandTypeSelect] = useState(isPreset ? initialBand : initialBand ? 'Custom' : '');
  const [bandTypeCustom, setBandTypeCustom] = useState(isPreset ? '' : initialBand);

  const [purchaseDate, setPurchaseDate] = useState(initial?.purchaseDate?.substring(0, 10) ?? '');
  const [purchasePrice, setPurchasePrice] = useState(initial?.purchasePrice?.toString() ?? '');
  const [notes, setNotes] = useState(initial?.notes ?? '');
  const [files, setFiles] = useState<File[]>([]);

  function handleFileChange(e: ChangeEvent<HTMLInputElement>) {
    if (e.target.files) {
      const selected = Array.from(e.target.files);
      setFiles((prev) => [...prev, ...selected]);
      e.target.value = '';
    }
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    const bandType = bandTypeSelect === 'Custom' ? bandTypeCustom : bandTypeSelect;
    const data: CreateWatch = {
      brand,
      model,
      movementType,
      ...(caseSizeMm && { caseSizeMm: Number(caseSizeMm) }),
      ...(bandType && { bandType }),
      ...(purchaseDate && { purchaseDate }),
      ...(purchasePrice && { purchasePrice: Number(purchasePrice) }),
      ...(notes && { notes }),
    };
    onSubmit(data, files);
  }

  return (
    <form className="watch-form" onSubmit={handleSubmit}>
      <fieldset className="watch-form-group">
        <legend>Watch Details</legend>
        <div className="watch-form-row">
          <label>
            Brand *
            <input value={brand} onChange={(e) => setBrand(e.target.value)} required />
          </label>
          <label>
            Model *
            <input value={model} onChange={(e) => setModel(e.target.value)} required />
          </label>
        </div>
        <div className="watch-form-row">
          <label>
            Movement Type *
            <select value={movementType} onChange={(e) => setMovementType(e.target.value as MovementType)}>
              {MOVEMENT_TYPES.map((mt) => (
                <option key={mt} value={mt}>{mt}</option>
              ))}
            </select>
          </label>
          <label>
            Case Size (mm)
            <input type="number" value={caseSizeMm} onChange={(e) => setCaseSizeMm(e.target.value)} />
          </label>
        </div>
        <div className="watch-form-row">
          <label>
            Band Type
            <select value={bandTypeSelect} onChange={(e) => setBandTypeSelect(e.target.value)}>
              <option value="">— Select —</option>
              {BAND_TYPE_OPTIONS.map((bt) => (
                <option key={bt} value={bt}>{bt}</option>
              ))}
              <option value="Custom">Custom…</option>
            </select>
          </label>
          {bandTypeSelect === 'Custom' && (
            <label>
              Custom Band Type
              <input value={bandTypeCustom} onChange={(e) => setBandTypeCustom(e.target.value)} />
            </label>
          )}
        </div>
      </fieldset>

      <fieldset className="watch-form-group">
        <legend>Purchase Info</legend>
        <div className="watch-form-row">
          <label>
            Purchase Date
            <input type="date" value={purchaseDate} onChange={(e) => setPurchaseDate(e.target.value)} />
          </label>
          <label>
            Purchase Price
            <input type="number" step="0.01" value={purchasePrice} onChange={(e) => setPurchasePrice(e.target.value)} />
          </label>
        </div>
      </fieldset>

      <fieldset className="watch-form-group">
        <legend>Additional</legend>
        <label>
          Notes
          <textarea value={notes} onChange={(e) => setNotes(e.target.value)} />
        </label>
        <label>
          Images
          <input type="file" accept="image/*" multiple onChange={handleFileChange} />
        </label>
        {files.length > 0 && <p className="file-count">{files.length} file(s) selected</p>}
      </fieldset>

      <div className="watch-form-actions">
        <button type="submit">{submitLabel}</button>
        {onCancel && <button type="button" className="btn btn-danger" onClick={onCancel}>Cancel</button>}
      </div>
    </form>
  );
}
