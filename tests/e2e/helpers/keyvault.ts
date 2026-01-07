import { DefaultAzureCredential } from '@azure/identity';
import { SecretClient } from '@azure/keyvault-secrets';

const KEYVAULT_NAME = 'ponovaweight-kv';
const KEYVAULT_URL = `https://${KEYVAULT_NAME}.vault.azure.net`;

let secretClient: SecretClient | null = null;

function getSecretClient(): SecretClient {
  if (!secretClient) {
    const credential = new DefaultAzureCredential();
    secretClient = new SecretClient(KEYVAULT_URL, credential);
  }
  return secretClient;
}

export async function getSecret(secretName: string): Promise<string> {
  const client = getSecretClient();
  const secret = await client.getSecret(secretName);
  if (!secret.value) {
    throw new Error(`Secret '${secretName}' not found or has no value`);
  }
  return secret.value;
}

export async function getTestCredentials(): Promise<{ email: string; password: string }> {
  const [email, password] = await Promise.all([
    getSecret('TestGoogleEmail'),
    getSecret('TestGooglePassword'),
  ]);
  return { email, password };
}
