#!/usr/bin/env node
/* Adds landing.navPricing to every locale. */
const fs   = require('fs');
const path = require('path');

const dir = path.join(__dirname, '..', 'src', 'assets', 'i18n');

const VALUES = {
    'en':    'Pricing',
    'fr':    'Tarifs',
    'de':    'Preise',
    'es':    'Precios',
    'ar':    'الأسعار',
    'fa':    'قیمت‌گذاری',
    'nl-BE': 'Prijzen',
    'zh':    '定价'
};

for (const [code, value] of Object.entries(VALUES)) {
    const p = path.join(dir, `${code}.json`);
    const j = JSON.parse(fs.readFileSync(p, 'utf8'));
    if (j.landing) {
        j.landing.navPricing = value;
        fs.writeFileSync(p, JSON.stringify(j, null, 4) + '\n');
        console.log(`✓ ${code}.json — landing.navPricing = "${value}"`);
    }
}
