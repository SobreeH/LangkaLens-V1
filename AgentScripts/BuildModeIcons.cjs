const fs = require('fs');
const path = require('path');
const sharp = require('C:/Users/sobre/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');

const source = 'Temp/UIReview/Tabler';
const editable = 'AgentScripts/ModeIcons';
const output = 'Assets/_LangkaLens/Main_menu/Images/ModeIcons';
fs.mkdirSync(editable, { recursive: true });
fs.mkdirSync(output, { recursive: true });
const read = name => fs.readFileSync(path.join(source, name + '.svg'), 'utf8');
const paths = svg => [...svg.matchAll(/<path\s+d="([^"]+)"\s*\/>/g)].map(m => `<path d="${m[1]}"/>`).join('\n');
const wrap = content => `<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 24 24" fill="none" stroke="#533A2D" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">${content}</svg>`;
const icons = {
  'VirtualTour': wrap(paths(read('view-360'))),
  'VirtualMuseum': wrap(paths(read('building-bank'))),
  'MixedReality': wrap(paths(read('scan')).replace('<path d="M5 12h14"/>', '') + `<g transform="translate(4.8 4.8) scale(.6)" stroke-width="2.83">${paths(read('cube'))}</g>`),
};

(async () => {
  for (const [name, svg] of Object.entries(icons)) {
    fs.writeFileSync(path.join(editable, name + '.svg'), svg);
    await sharp(Buffer.from(svg)).png().toFile(path.join(output, name + '.png'));
    console.log(name + ': transparent 512 x 512 PNG');
  }
  fs.copyFileSync(path.join(source, 'LICENSE.txt'), path.join(editable, 'LICENSE.txt'));
  fs.writeFileSync(path.join(editable, 'README.md'), `# Main menu mode icons\n\nAdapted from [Tabler Icons](https://github.com/tabler/tabler-icons), licensed under MIT (see LICENSE.txt).\n\n- VirtualTour: outline/view-360.svg\n- VirtualMuseum: outline/building-bank.svg\n- MixedReality: outline/scan.svg (corner frame) plus outline/cube.svg (scaled inset)\n\nAll use matching rounded strokes in #533A2D on transparent backgrounds.\nEditable SVG sources live here; Unity consumes the 512px PNGs in Assets/_LangkaLens/Main_menu/Images/ModeIcons.\n`);
})();
