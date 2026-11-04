// Downloads the running API's OpenAPI document into openapi/support-triage.v1.json.
// The .NET dev certificate is self-signed, so certificate checks are skipped, and only for localhost.
import { writeFile } from 'node:fs/promises';
import { get } from 'node:https';

const source = process.argv[2] ?? 'https://localhost:51053/openapi/v1.json';
const target = new URL('../openapi/support-triage.v1.json', import.meta.url);
const url = new URL(source);

if (url.hostname !== 'localhost') {
  throw new Error(`Refusing to skip certificate checks for ${url.hostname}; only localhost is allowed.`);
}

const body = await new Promise((resolve, reject) => {
  get(url, { rejectUnauthorized: false }, (response) => {
    if (response.statusCode !== 200) {
      reject(new Error(`${source} answered ${response.statusCode}. Is the API running?`));
      return;
    }
    const chunks = [];
    response.on('data', (chunk) => chunks.push(chunk));
    response.on('end', () => resolve(Buffer.concat(chunks).toString('utf8')));
  }).on('error', reject);
});

await writeFile(target, JSON.stringify(JSON.parse(body), null, 2) + '\n');
console.log(`Saved ${source} to openapi/support-triage.v1.json`);
