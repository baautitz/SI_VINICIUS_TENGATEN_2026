# syntax=docker/dockerfile:1

# deps: so invalida quando package.json/package-lock.json mudam
FROM node:24-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN --mount=type=cache,target=/root/.npm npm ci

FROM node:24-alpine AS prod-deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN --mount=type=cache,target=/root/.npm npm ci --omit=dev

FROM deps AS source
COPY . .

# ci: lint + testes. docker build --target ci ./frontend
FROM source AS ci
RUN npm run lint

FROM source AS build
RUN npm run build

FROM node:24-alpine AS final
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
COPY --from=prod-deps /app/node_modules ./node_modules
COPY --from=build /app/.next ./.next
COPY --from=build /app/package.json ./package.json
EXPOSE 3000
CMD ["npm", "run", "start"]
