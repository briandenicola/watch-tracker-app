FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm ci
COPY . .
ENV VITE_API_BASE_URL=""
RUN npm run build

FROM node:22-alpine
WORKDIR /app
COPY --from=build /app/dist ./dist
COPY --from=build /app/server.js .
COPY --from=build /app/package.json .
COPY --from=build /app/package-lock.json* ./
RUN npm ci --omit=dev
ENV PORT=80
EXPOSE 80
CMD ["node", "server.js"]
