const { Client } = require('pg');
const client = new Client({
  host: 'localhost',
  port: 5432,
  database: 'criminal_network_db',
  user: 'postgres',
  password: 'postgres'
});

async function run() {
  await client.connect();
  const res = await client.query('SELECT "BlockIndex", "Id", "EvidenceItemId", "BlockHash", "PreviousBlockHash", "TimestampUtc", "MetadataJson", "Action", "ActorUserId", "EvidenceHash" FROM "EvidenceLedgerBlocks" ORDER BY "BlockIndex" ASC;');
  console.log('Total blocks:', res.rows.length);
  for (const row of res.rows) {
    console.log(JSON.stringify(row));
  }
  await client.end();
}

run().catch(console.error);
