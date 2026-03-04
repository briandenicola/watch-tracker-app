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
  const [linkUrl, setLinkUrl] = useState(initial?.linkUrl ?? '');
  const [linkText, setLinkText] = useState(initial?.linkText ?? 'Product Page');
  const [files, setFiles] = useState<File[]>([]);

  // Additional detail fields
  const [crystalType, setCrystalType] = useState(initial?.crystalType ?? '');
  const [caseShape, setCaseShape] = useState(initial?.caseShape ?? '');
  const [crownType, setCrownType] = useState(initial?.crownType ?? '');
  const [calendarType, setCalendarType] = useState(initial?.calendarType ?? '');
  const [countryOfOrigin, setCountryOfOrigin] = useState(initial?.countryOfOrigin ?? '');
  const [waterResistance, setWaterResistance] = useState(initial?.waterResistance ?? '');
  const [lugWidthMm, setLugWidthMm] = useState(initial?.lugWidthMm?.toString() ?? '');
  const [dialColor, setDialColor] = useState(initial?.dialColor ?? '');
  const [bezelType, setBezelType] = useState(initial?.bezelType ?? '');
  const [powerReserveHours, setPowerReserveHours] = useState(initial?.powerReserveHours?.toString() ?? '');
  const [serialNumber, setSerialNumber] = useState(initial?.serialNumber ?? '');
  const [detailsOpen, setDetailsOpen] = useState(false);

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
      ...(crystalType && { crystalType }),
      ...(caseShape && { caseShape }),
      ...(crownType && { crownType }),
      ...(calendarType && { calendarType }),
      ...(countryOfOrigin && { countryOfOrigin }),
      ...(waterResistance && { waterResistance }),
      ...(lugWidthMm && { lugWidthMm: Number(lugWidthMm) }),
      ...(dialColor && { dialColor }),
      ...(bezelType && { bezelType }),
      ...(powerReserveHours && { powerReserveHours: Number(powerReserveHours) }),
      ...(serialNumber && { serialNumber }),
      ...(linkUrl && { linkUrl }),
      ...(linkUrl && linkText && { linkText }),
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

      <div className="accordion">
        <button type="button" className="accordion-toggle" onClick={() => setDetailsOpen(!detailsOpen)}>
          Additional Details
          <span className={`accordion-chevron${detailsOpen ? ' open' : ''}`}>▼</span>
        </button>
        <div className={`accordion-content${detailsOpen ? ' open' : ''}`}>
          <div className="accordion-inner">
            <div className="watch-form-row" style={{ marginTop: '0.75rem' }}>
              <label>
                Crystal Type
                <input value={crystalType} onChange={(e) => setCrystalType(e.target.value)} placeholder="e.g. Sapphire" />
              </label>
              <label>
                Case Shape
                <input value={caseShape} onChange={(e) => setCaseShape(e.target.value)} placeholder="e.g. Round" />
              </label>
            </div>
            <div className="watch-form-row">
              <label>
                Crown Type
                <input value={crownType} onChange={(e) => setCrownType(e.target.value)} placeholder="e.g. Screw-down" />
              </label>
              <label>
                Calendar Type
                <input value={calendarType} onChange={(e) => setCalendarType(e.target.value)} placeholder="e.g. Date" />
              </label>
            </div>
            <div className="watch-form-row">
              <label>
                Country of Origin
                <input value={countryOfOrigin} onChange={(e) => setCountryOfOrigin(e.target.value)} placeholder="e.g. Switzerland" />
              </label>
              <label>
                Water Resistance
                <input value={waterResistance} onChange={(e) => setWaterResistance(e.target.value)} placeholder="e.g. 100m" />
              </label>
            </div>
            <div className="watch-form-row">
              <label>
                Lug Width (mm)
                <input type="number" value={lugWidthMm} onChange={(e) => setLugWidthMm(e.target.value)} />
              </label>
              <label>
                Dial Color
                <input value={dialColor} onChange={(e) => setDialColor(e.target.value)} placeholder="e.g. Black" />
              </label>
            </div>
            <div className="watch-form-row">
              <label>
                Bezel Type
                <input value={bezelType} onChange={(e) => setBezelType(e.target.value)} placeholder="e.g. Rotating" />
              </label>
              <label>
                Power Reserve (hours)
                <input type="number" value={powerReserveHours} onChange={(e) => setPowerReserveHours(e.target.value)} />
              </label>
            </div>
            <div className="watch-form-row">
              <label>
                Serial / Reference Number
                <input value={serialNumber} onChange={(e) => setSerialNumber(e.target.value)} />
              </label>
              <label>{/* spacer */}</label>
            </div>
            <label style={{ marginTop: '0.75rem' }}>
              Notes
              <textarea value={notes} onChange={(e) => setNotes(e.target.value)} />
            </label>
          </div>
        </div>
      </div>

      <fieldset className="watch-form-group">
        <legend>Link</legend>
        <div className="watch-form-row">
          <label>
            URL
            <input type="url" value={linkUrl} onChange={(e) => setLinkUrl(e.target.value)} placeholder="https://…" />
          </label>
          <label>
            Display Text
            <input value={linkText} onChange={(e) => setLinkText(e.target.value)} placeholder="e.g. Product page" />
          </label>
        </div>
      </fieldset>

      <fieldset className="watch-form-group">
        <legend>Images</legend>
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
