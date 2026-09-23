export interface WatchHelpTerm {
  name: string
  description: string
  example?: string
}

export interface WatchHelpSection {
  title: string
  description: string
  terms: WatchHelpTerm[]
}

export const watchHelpSections: WatchHelpSection[] = [
  {
    title: 'Identification',
    description: 'The details that identify a specific watch and distinguish it from similar models.',
    terms: [
      { name: 'Brand', description: 'The company or maker whose name appears on the watch.', example: 'Omega, Rolex, Seiko' },
      { name: 'Model', description: 'The product family or model name assigned by the maker.', example: 'Speedmaster Professional' },
      { name: 'Category', description: 'The watch’s general style or intended use.', example: 'Diver, dress, field, pilot' },
      { name: 'SKU / Reference', description: 'The maker’s reference number for this exact version. Unlike a serial number, many identical watches share it.', example: '126610LN' },
      { name: 'Serial Number', description: 'A unique identifier assigned to this individual watch.' },
      { name: 'Production Year', description: 'The year the watch was manufactured, which may differ from the year it was purchased.' },
      { name: 'Country of Origin', description: 'The country where the watch or its movement was made, as stated by the manufacturer.', example: 'Switzerland, Japan, Germany' },
      { name: 'Photo', description: 'An image used to recognize the watch in your collection. You can upload one or import one from a public image URL.' },
    ],
  },
  {
    title: 'Case & Dial',
    description: 'Measurements and visible parts of the watch head. Measurements are recorded in millimeters (mm).',
    terms: [
      { name: 'Case Size', description: 'The width of the main watch case, usually measured across the dial from roughly 9 o’clock to 3 o’clock, excluding the crown.', example: '40 mm' },
      { name: 'Case Thickness', description: 'The height of the watch from the case back to the highest point of the crystal.' },
      { name: 'Lug Width', description: 'The inside distance between the two lugs where the strap or bracelet attaches. Use it to find a correctly sized replacement band.', example: '20 mm' },
      { name: 'Lug-to-Lug', description: 'The full distance from the tip of the upper lugs to the tip of the lower lugs. It is a key indicator of how long the watch will sit across your wrist.', example: '47.5 mm' },
      { name: 'Case Material', description: 'The material used for the watch’s outer body.', example: 'Stainless steel, titanium, ceramic, gold' },
      { name: 'Case Shape', description: 'The outline of the main case when viewed from the front.', example: 'Round, square, rectangular, tonneau' },
      { name: 'Crystal Type', description: 'The transparent cover over the dial. Sapphire is highly scratch-resistant; mineral glass and acrylic are softer but can be easier or cheaper to repair.' },
      { name: 'Bezel Type', description: 'The ring around the crystal. It may be fixed or rotate and can carry markings for timing, a second time zone, speed, or another scale.', example: 'Fixed, ceramic dive, GMT, tachymeter' },
      { name: 'Crown Type', description: 'The small knob, usually on the side of the case, used to set the time and often to wind the watch. A screw-down crown threads into the case to improve water sealing.', example: 'Push/pull, screw-down' },
      { name: 'Dial Color', description: 'The main visible color of the watch face beneath the hands and markers.', example: 'Black, blue sunburst, silver' },
      { name: 'Water Resistance', description: 'The manufacturer’s rated resistance to water pressure. It is not a guarantee for an aging watch and should not be treated as a literal safe diving depth.', example: '50 m, 100 m, 10 ATM' },
    ],
  },
  {
    title: 'Band',
    description: 'The strap or bracelet that secures the watch to your wrist.',
    terms: [
      { name: 'Band Type', description: 'The construction or material of the strap or bracelet.', example: 'Steel bracelet, leather, rubber, NATO, mesh' },
      { name: 'Band Color', description: 'The primary visible color of the current strap or bracelet.' },
    ],
  },
  {
    title: 'Movement & Power',
    description: 'How the watch keeps time and which features its mechanism provides.',
    terms: [
      { name: 'Movement', description: 'The mechanism that powers the watch. Automatic movements wind from wrist motion, manual movements are wound by hand, quartz movements use a battery, and digital describes an electronic display.' },
      { name: 'Power Reserve', description: 'How many hours a fully powered mechanical watch should continue running when it is not worn or wound.', example: '70 hours' },
      { name: 'Calendar Type', description: 'The overall calendar display or mechanism included in the watch.', example: 'Date, day-date, annual calendar, perpetual calendar' },
      { name: 'Date Complication', description: 'The specific date-related information shown beyond basic timekeeping. In watchmaking, a complication is any function beyond displaying hours, minutes, and seconds.', example: 'Date window, pointer date, day-date' },
      { name: 'Battery Type', description: 'The replacement battery code required by a quartz or digital watch.', example: 'SR626SW' },
      { name: 'Last Battery Changed', description: 'The date the watch’s battery was most recently replaced.' },
      { name: 'Winder TPD', description: 'Turns per day: the number of rotations an automatic watch winder should make in 24 hours to keep this movement powered.' },
      { name: 'Winder Direction', description: 'The direction in which a watch winder should rotate for this movement.', example: 'Clockwise, counterclockwise, bidirectional' },
    ],
  },
  {
    title: 'Purchase & Ownership',
    description: 'When, where, and how the watch became part of your collection or wish list.',
    terms: [
      { name: 'Purchase Date', description: 'The date you acquired or paid for the watch.' },
      { name: 'Purchase Price', description: 'The amount you paid for a watch in your collection.' },
      { name: 'Target Price', description: 'The price you hope to pay for a watch on your wish list.' },
      { name: 'Acquisition Type', description: 'How you obtained the watch.', example: 'New, used, trade, other' },
      { name: 'Acquired From', description: 'The retailer, marketplace, auction, or person from whom you obtained the watch.' },
      { name: 'Acquisition Source URL', description: 'A link to the original sale, listing, receipt, or source used when acquiring the watch.' },
      { name: 'Warranty Expiry', description: 'The last date through which the manufacturer or seller’s warranty is expected to apply.' },
      { name: 'Last Serviced', description: 'The date the movement or watch was most recently professionally serviced.' },
    ],
  },
  {
    title: 'Storage, Links & Notes',
    description: 'Private organizational details and supporting references.',
    terms: [
      { name: 'Storage Location', description: 'Where you keep the watch. Available locations are managed in Settings.', example: 'Watch box, safe, winder' },
      { name: 'Product / Reference URL', description: 'A useful link to the manufacturer’s product page or another reference about the model.' },
      { name: 'Product Link Text', description: 'The short label displayed instead of the full product or reference URL.' },
      { name: 'Notes', description: 'Any additional details you want to remember. Notes support Markdown formatting.' },
    ],
  },
]
