import { fileURLToPath, URL } from 'node:url';

import plugin from '@vitejs/plugin-react';
import child_process from 'child_process';
import fs from 'fs';
import path from 'path';
import { env } from 'process';
import { defineConfig } from 'vite';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = "mangareader_managerui.client";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

if (!fs.existsSync(baseFolder)) {
    fs.mkdirSync(baseFolder, { recursive: true });
}

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    if (0 !== child_process.spawnSync('dotnet', [
        'dev-certs',
        'https',
        '--export-path',
        certFilePath,
        '--format',
        'Pem',
        '--no-password',
    ], { stdio: 'inherit', }).status) {
        throw new Error("Could not create certificate.");
    }
}

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7242';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        proxy: {
            // Thay đổi proxy để chuyển tiếp tất cả các request bắt đầu bằng '/api'
            '^/api': { // Vẫn giữ regex này để khớp với /api
                target,
                secure: false, // Để bỏ qua việc kiểm tra chứng chỉ SSL nếu bạn đang sử dụng HTTPS cục bộ
                ws: true, // Hỗ trợ WebSockets, cần thiết cho HMR (Hot Module Replacement)
                rewrite: (path) => path, 
            },
            // Giữ nguyên proxy cũ nếu bạn vẫn cần nó cho weatherforecast
            // '^/weatherforecast': { // Có thể xóa nếu không sử dụng endpoint này nữa
            //     target,
            //     secure: false
            // }
        },
        port: parseInt(env.DEV_SERVER_PORT || '49820'),
        https: {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        }
    }
})