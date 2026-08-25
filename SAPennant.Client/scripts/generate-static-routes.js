// Postbuild: emit a static HTML file per tab route so GitHub Pages serves
// them with true 200s (instead of the 404.html SPA fallback), each with a
// route-specific title, description, and canonical URL for SEO.
//
// Creates both <route>.html (served for /route) and <route>/index.html
// (served for /route/), so either URL form returns 200.

const fs = require('fs');
const path = require('path');

const DIST = path.join(__dirname, '..', 'dist', 'SAPennant', 'browser');
const BASE_URL = 'https://sapennantgolf.com';

const ROUTES = {
  'search': {
    title: 'Player Search | SA Pennant Golf',
    description: 'Search any SA Pennant golf player’s match history, results and stats across regular season and finals, 2021–2026.',
  },
  'club': {
    title: 'Club Search | SA Pennant Golf',
    description: 'Browse SA Pennant golf results and player records by club.',
  },
  'leaderboard': {
    title: 'Leaderboard | SA Pennant Golf',
    description: 'SA Pennant golf player leaderboards by season, division and pool — wins, losses and win rates.',
  },
  'handicap': {
    title: 'Handicap Tracker | SA Pennant Golf',
    description: 'Track SA Pennant golf players’ handicap movement across seasons.',
  },
  'honour-roll': {
    title: 'Honour Roll | SA Pennant Golf',
    description: 'Premiership winners across SA Pennant golf competitions and seasons.',
  },
  'admin': {
    title: 'Admin | SA Pennant Golf',
    description: null,
    noindex: true,
  },
};

const indexPath = path.join(DIST, 'index.html');
const indexHtml = fs.readFileSync(indexPath, 'utf8');

for (const [route, meta] of Object.entries(ROUTES)) {
  let html = indexHtml;
  html = html.replace(/<title>[^<]*<\/title>/, `<title>${meta.title}</title>`);
  if (meta.description) {
    html = html.replace(
      /(<meta[^>]*name="description"[^>]*content=")[^"]*(")/,
      `$1${meta.description}$2`
    );
  }

  const headExtras = [`<link rel="canonical" href="${BASE_URL}/${route}">`];
  if (meta.noindex) headExtras.push('<meta name="robots" content="noindex">');
  html = html.replace('</head>', `${headExtras.join('')}</head>`);

  fs.writeFileSync(path.join(DIST, `${route}.html`), html);
  fs.mkdirSync(path.join(DIST, route), { recursive: true });
  fs.writeFileSync(path.join(DIST, route, 'index.html'), html);
  console.log(`static route: /${route}`);
}

// Root page gets its canonical too.
fs.writeFileSync(indexPath, indexHtml.replace('</head>', `<link rel="canonical" href="${BASE_URL}/"></head>`));
console.log('static routes generated');
