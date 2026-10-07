import { connect, createServer } from 'node:net';
import { describe, expect, it } from 'vitest';
import { WebSocket as NodeWebSocket } from 'ws';

describe('production bridge runtime', () => {
    it('uses loopback by default and advertises only its reachable local URL', async () => {
        const bridge = require('../../dist/bridge.cjs');
        const port = await getFreePort();
        const runtime = bridge.createBridgeRuntime({ port, maxPort: port });
        try {
            await waitUntil(() => runtime.listening);
            expect(runtime.advertisedUrls).toEqual([`http://127.0.0.1:${port}/remote/`]);
            expect(runtime.remoteUrl).toBe(`http://127.0.0.1:${port}/remote/`);
            const response = await fetch(`http://127.0.0.1:${port}/remote/api/health`);
            const health = (await response.json()) as Record<string, unknown>;
            expect(health.ok).toBe(true);
            expect(health.port).toBe(port);
            expect(health.protocolVersion).toBe(1);
            expect(health.installedAppsReady).toBe(false);
            runtime.syncInstalledApps({ apps: [] });
            const ready = await fetch(`http://127.0.0.1:${port}/remote/api/health`);
            expect(((await ready.json()) as Record<string, unknown>).installedAppsReady).toBe(true);
        } finally {
            runtime.close();
        }
    });

    it('reports the actual port after the preferred port is occupied', async () => {
        const bridge = require('../../dist/bridge.cjs');
        const blocker = createServer();
        await new Promise<void>((resolve) => blocker.listen(0, '127.0.0.1', resolve));
        const address = blocker.address();
        if (!address || typeof address === 'string') throw new Error('No blocker address.');
        const port = address.port;
        const runtime = bridge.createBridgeRuntime({ port, maxPort: port + 30 });
        try {
            await waitUntil(() => runtime.listening);
            const actual = new URL(runtime.remoteUrl);
            expect(Number(actual.port)).toBeGreaterThan(port);
            const health = await fetch(new URL('api/health', actual));
            expect(((await health.json()) as Record<string, unknown>).port).toBe(
                Number(actual.port),
            );
        } finally {
            runtime.close();
            await new Promise<void>((resolve) => blocker.close(() => resolve()));
        }
    });

    it('preserves the public API and sends cached snapshots after hello', async () => {
        const bridge = require('../../dist/bridge.cjs');
        expect(Object.keys(bridge).sort()).toEqual([
            'createBridgeRuntime',
            'ensureBridge',
            'installWandRuntime',
        ]);

        const port = await getFreePort();
        const runtime = bridge.createBridgeRuntime({ host: '127.0.0.1', port, maxPort: port });
        runtime.sync(rawTrainerSnapshot());
        runtime.syncInstalledApps({
            apps: [
                {
                    platform: 'steam',
                    sku: '123',
                    displayName: 'Game',
                    location: 'C:\\private\\Game',
                    alternateLocations: ['D:\\also-private\\Game'],
                },
            ],
        });

        try {
            await waitUntil(() => runtime.listening);
            const messages = await connectAndCollect(port, 4);
            expect(messages.map((message) => message.type)).toEqual([
                'hello_ack',
                'trainer_meta',
                'trainer_values',
                'installed_apps',
            ]);
            expect(messages[2].payload.values.god).toBe(true);
            expect(JSON.stringify(messages)).not.toContain('wand-secret');
            expect(JSON.stringify(messages)).not.toContain('private');
        } finally {
            runtime.close();
        }
    });

    it('rejects a browser WebSocket from a different origin', async () => {
        const bridge = require('../../dist/bridge.cjs');
        const port = await getFreePort();
        const runtime = bridge.createBridgeRuntime({ host: '127.0.0.1', port, maxPort: port });

        try {
            await waitUntil(() => runtime.listening);
            await expectUpgradeStatus(port, 403);
        } finally {
            runtime.close();
        }
    });

    it('allows the local Vite panel to connect to the loopback bridge', async () => {
        const bridge = require('../../dist/bridge.cjs');
        const port = await getFreePort();
        const runtime = bridge.createBridgeRuntime({ host: '127.0.0.1', port, maxPort: port });

        try {
            await waitUntil(() => runtime.listening);
            await expectUpgradeAccepted(port, 'http://127.0.0.1:4173');
        } finally {
            runtime.close();
        }
    });

    it('closes an oversized WebSocket frame without buffering its payload', async () => {
        const bridge = require('../../dist/bridge.cjs');
        const port = await getFreePort();
        const runtime = bridge.createBridgeRuntime({ host: '127.0.0.1', port, maxPort: port });

        try {
            await waitUntil(() => runtime.listening);
            await expectSocketClose(port, Buffer.alloc(1024 * 1024 + 1), 1009);
            await expectDeclaredHugeFrameClose(port, 1009);
        } finally {
            runtime.close();
        }
    });

    it('does not trust the HTTP Host header as a URL base', async () => {
        const bridge = require('../../dist/bridge.cjs');
        const port = await getFreePort();
        const runtime = bridge.createBridgeRuntime({ host: '127.0.0.1', port, maxPort: port });

        try {
            await waitUntil(() => runtime.listening);
            const malformedHost = await sendRawHttp(
                port,
                'GET /remote/api/health HTTP/1.1\r\nHost: [\r\nConnection: close\r\n\r\n',
            );
            expect(malformedHost).toContain('HTTP/1.1 200 OK');

            const malformedTarget = await sendRawHttp(
                port,
                'GET //[ HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n',
            );
            expect(malformedTarget).toContain('HTTP/1.1 400 Bad Request');
        } finally {
            runtime.close();
        }
    });
});

async function getFreePort(): Promise<number> {
    return await new Promise((resolve, reject) => {
        const server = createServer();
        server.once('error', reject);
        server.listen(0, '127.0.0.1', () => {
            const address = server.address();
            const port = typeof address === 'object' && address ? address.port : 0;
            server.close((error) => (error ? reject(error) : resolve(port)));
        });
    });
}

async function connectAndCollect(port: number, count: number): Promise<any[]> {
    return await new Promise((resolve, reject) => {
        const messages: any[] = [];
        const socket = new NodeWebSocket(`ws://127.0.0.1:${port}/remote/ws`);
        socket.once('error', reject);
        socket.once('open', () =>
            socket.send(
                JSON.stringify({
                    type: 'hello',
                    version: 1,
                    requestId: 'hello',
                    payload: {
                        client: 'mobile-web',
                        clientVersion: 'test',
                        capabilities: { supportsDeltaValues: true, supportsTrainerSwitch: true },
                    },
                }),
            ),
        );
        socket.on('message', (raw) => {
            messages.push(JSON.parse(String(raw)));
            if (messages.length === count) {
                socket.close();
                resolve(messages);
            }
        });
    });
}

async function expectUpgradeStatus(port: number, expectedStatus: number): Promise<void> {
    await new Promise<void>((resolve, reject) => {
        const socket = new NodeWebSocket(`ws://127.0.0.1:${port}/remote/ws`, {
            headers: { Origin: 'https://example.com' },
        });
        socket.once('open', () => reject(new Error('Cross-origin WebSocket was accepted.')));
        socket.once('error', () => undefined);
        socket.once('unexpected-response', (_request, response) => {
            response.resume();
            if (response.statusCode === expectedStatus) {
                resolve();
            } else {
                reject(new Error(`Expected HTTP ${expectedStatus}, got ${response.statusCode}.`));
            }
        });
    });
}

async function expectUpgradeAccepted(port: number, origin: string): Promise<void> {
    await new Promise<void>((resolve, reject) => {
        const socket = new NodeWebSocket(`ws://127.0.0.1:${port}/remote/ws`, {
            headers: { Origin: origin },
        });
        socket.once('open', () => {
            socket.close();
            resolve();
        });
        socket.once('error', reject);
    });
}

async function expectSocketClose(
    port: number,
    payload: Buffer,
    expectedCode: number,
): Promise<void> {
    await new Promise<void>((resolve, reject) => {
        const socket = new NodeWebSocket(`ws://127.0.0.1:${port}/remote/ws`);
        socket.once('open', () => socket.send(payload));
        socket.once('close', (code) =>
            code === expectedCode
                ? resolve()
                : reject(new Error(`Expected close code ${expectedCode}, got ${code}.`)),
        );
        socket.once('error', reject);
    });
}

async function expectDeclaredHugeFrameClose(port: number, expectedCode: number): Promise<void> {
    await new Promise<void>((resolve, reject) => {
        const socket = new NodeWebSocket(`ws://127.0.0.1:${port}/remote/ws`);
        socket.once('open', () => {
            const header = Buffer.alloc(10);
            header[0] = 0x81;
            header[1] = 0xff;
            header.writeUInt32BE(1, 2);
            (socket as any)._socket.write(header);
        });
        socket.once('close', (code) =>
            code === expectedCode
                ? resolve()
                : reject(new Error(`Expected close code ${expectedCode}, got ${code}.`)),
        );
        socket.once('error', reject);
    });
}

async function sendRawHttp(port: number, request: string): Promise<string> {
    return await new Promise<string>((resolve, reject) => {
        const chunks: Buffer[] = [];
        const socket = connect(port, '127.0.0.1');
        socket.once('connect', () => socket.end(request));
        socket.on('data', (chunk) => chunks.push(chunk));
        socket.once('end', () => resolve(Buffer.concat(chunks).toString('utf8')));
        socket.once('error', reject);
    });
}

async function waitUntil(predicate: () => boolean): Promise<void> {
    const deadline = Date.now() + 3000;
    while (!predicate()) {
        if (Date.now() > deadline) throw new Error('Bridge did not start listening.');
        await new Promise((resolve) => setTimeout(resolve, 10));
    }
}

function rawTrainerSnapshot() {
    return {
        instanceId: 'instance',
        accessToken: 'wand-secret',
        trainerId: 'trainer',
        trainerInfo: { gameId: 'game', displayName: 'Game' },
        metadata: {
            info: {
                blueprint: {
                    cheats: [
                        {
                            uuid: 'god',
                            target: 'god',
                            type: 'toggle',
                            name: 'God mode',
                            category: 'player',
                            args: {},
                        },
                    ],
                },
            },
        },
        values: { god: 1 },
    };
}
