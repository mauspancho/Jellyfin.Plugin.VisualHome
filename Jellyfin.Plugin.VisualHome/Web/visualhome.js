(function () {
    'use strict';

    const state = {
        rendering: false,
        lastPath: '',
        observer: null,
        apiWaits: 0
    };

    const rootId = 'vh-home-root';
    const diagnosticId = 'vh-diagnostic';

    function value(source, camelName, pascalName, fallback) {
        if (!source) {
            return fallback;
        }

        if (source[camelName] !== undefined) {
            return source[camelName];
        }

        if (source[pascalName] !== undefined) {
            return source[pascalName];
        }

        return fallback;
    }

    function normalizeItem(item) {
        return {
            id: value(item, 'id', 'Id', ''),
            name: value(item, 'name', 'Name', ''),
            url: value(item, 'url', 'Url', ''),
            overview: value(item, 'overview', 'Overview', ''),
            productionYear: value(item, 'productionYear', 'ProductionYear', null),
            officialRating: value(item, 'officialRating', 'OfficialRating', ''),
            imagePrimaryUrl: value(item, 'imagePrimaryUrl', 'ImagePrimaryUrl', ''),
            imageBackdropUrl: value(item, 'imageBackdropUrl', 'ImageBackdropUrl', ''),
            genres: value(item, 'genres', 'Genres', [])
        };
    }

    function normalizeSection(section) {
        const items = value(section, 'items', 'Items', []) || [];
        return {
            sectionId: value(section, 'sectionId', 'SectionId', ''),
            name: value(section, 'name', 'Name', ''),
            visualType: value(section, 'visualType', 'VisualType', 'carousel'),
            position: value(section, 'position', 'Position', 0),
            success: value(section, 'success', 'Success', true),
            items: items.map(normalizeItem)
        };
    }

    function normalizeClientConfig(clientConfig) {
        return {
            pluginEnabled: value(clientConfig, 'pluginEnabled', 'PluginEnabled', false) === true,
            visualInjectionEnabled: value(clientConfig, 'visualInjectionEnabled', 'VisualInjectionEnabled', false) === true,
            sidebarEnabled: value(clientConfig, 'sidebarEnabled', 'SidebarEnabled', false) === true
        };
    }

    function pluginUrl(path) {
        if (window.ApiClient && ApiClient.getUrl) {
            return ApiClient.getUrl(path);
        }

        const basePath = location.pathname.includes('/web/')
            ? location.pathname.slice(0, location.pathname.indexOf('/web/'))
            : '';
        return basePath + '/' + path.replace(/^\/+/, '');
    }

    function api(path) {
        const url = pluginUrl(path);
        if (window.ApiClient && ApiClient.ajax) {
            return ApiClient.ajax({ type: 'GET', url: url, dataType: 'json' });
        }

        return fetch(url, { credentials: 'same-origin' }).then(response => response.json());
    }

    function currentUserId() {
        try {
            if (window.ApiClient && ApiClient.getCurrentUserId) {
                return ApiClient.getCurrentUserId();
            }

            if (window.ApiClient && ApiClient._serverInfo && ApiClient._serverInfo.UserId) {
                return ApiClient._serverInfo.UserId;
            }
        } catch (error) {
            console.warn('[VisualHome] user id lookup failed', error);
        }

        return '';
    }

    function withUser(path) {
        const userId = currentUserId();
        if (!userId) {
            return path;
        }

        return path + (path.includes('?') ? '&' : '?') + 'userId=' + encodeURIComponent(userId);
    }

    function isHomePage() {
        const hash = (location.hash || '').toLowerCase();
        const path = (location.pathname || '').toLowerCase();
        return hash === '' ||
            hash === '#!/home.html' ||
            hash === '#/home.html' ||
            hash === '#!/home' ||
            hash === '#/home' ||
            hash.includes('/home.html') ||
            hash.includes('home') ||
            path.endsWith('/web/') ||
            path.endsWith('/web/index.html');
    }

    function findHomeHost() {
        return document.querySelector('.homeSectionsContainer') ||
            document.querySelector('.sections') ||
            document.querySelector('[data-role="content"]') ||
            document.querySelector('.page:not(.hide) .pageContent') ||
            document.body;
    }

    function ensureStylesheet() {
        if (document.querySelector('link[data-vh-css]')) {
            return;
        }

        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = pluginUrl('VisualHome/assets/visualhome.css');
        link.dataset.vhCss = 'true';
        document.head.appendChild(link);
    }

    function removeRoot() {
        const existing = document.getElementById(rootId);
        if (existing) {
            existing.remove();
        }

        const diagnostic = document.getElementById(diagnosticId);
        if (diagnostic) {
            diagnostic.remove();
        }
    }

    function renderDiagnostic(message) {
        const host = findHomeHost();
        let diagnostic = document.getElementById(diagnosticId);
        if (!diagnostic) {
            diagnostic = document.createElement('div');
            diagnostic.id = diagnosticId;
            diagnostic.className = 'vh-diagnostic';
            host.prepend(diagnostic);
        }

        diagnostic.textContent = message;
    }

    function itemImage(url) {
        return url ? url : '';
    }

    function openItem(item) {
        if (!item || !item.url) {
            return;
        }

        location.hash = item.url;
    }

    function createButton(label, className, onClick) {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = className;
        button.textContent = label;
        button.addEventListener('click', onClick);
        return button;
    }

    function renderHero(section) {
        const item = section.items && section.items[0];
        if (!item) {
            return null;
        }

        const hero = document.createElement('section');
        hero.className = 'vh-hero';
        hero.style.backgroundImage = 'linear-gradient(90deg, rgba(10, 11, 16, .96), rgba(10, 11, 16, .58), rgba(10, 11, 16, .18)), url("' + itemImage(item.imageBackdropUrl || item.imagePrimaryUrl) + '")';

        const content = document.createElement('div');
        content.className = 'vh-hero-content';

        const title = document.createElement('h2');
        title.className = 'vh-hero-title';
        title.textContent = item.name;

        const meta = document.createElement('div');
        meta.className = 'vh-meta';
        meta.textContent = [item.productionYear, item.officialRating, (item.genres || []).slice(0, 3).join(' · ')].filter(Boolean).join(' · ');

        const overview = document.createElement('p');
        overview.className = 'vh-overview';
        overview.textContent = item.overview || '';

        const actions = document.createElement('div');
        actions.className = 'vh-actions';
        actions.appendChild(createButton('Ver ahora', 'vh-pill vh-pill-primary', () => openItem(item)));
        actions.appendChild(createButton('Detalles', 'vh-pill', () => openItem(item)));

        content.append(title, meta, overview, actions);
        hero.appendChild(content);
        return hero;
    }

    function renderCard(item, index, top10) {
        const card = document.createElement('button');
        card.type = 'button';
        card.className = top10 ? 'vh-card vh-top10-card' : 'vh-card';
        card.addEventListener('click', () => openItem(item));

        if (top10) {
            const rank = document.createElement('span');
            rank.className = 'vh-rank';
            rank.textContent = String(index + 1);
            card.appendChild(rank);
        }

        const poster = document.createElement('span');
        poster.className = 'vh-poster';
        poster.style.backgroundImage = 'url("' + itemImage(item.imagePrimaryUrl) + '")';

        const name = document.createElement('span');
        name.className = 'vh-card-name';
        name.textContent = item.name;

        card.append(poster, name);
        return card;
    }

    function renderRow(section) {
        if (!section.items || !section.items.length) {
            return null;
        }

        const block = document.createElement('section');
        block.className = 'vh-section vh-' + (section.visualType || 'carousel');

        const title = document.createElement('h2');
        title.className = 'vh-section-title';
        title.textContent = section.name;

        const row = document.createElement('div');
        row.className = section.visualType === 'grid' ? 'vh-grid' : 'vh-carousel';
        section.items.forEach((item, index) => row.appendChild(renderCard(item, index, section.visualType === 'top10')));

        block.append(title, row);
        return block;
    }

    function renderStudios(section) {
        const block = document.createElement('section');
        block.className = 'vh-section vh-studios';

        const title = document.createElement('h2');
        title.className = 'vh-section-title';
        title.textContent = section.name;

        const row = document.createElement('div');
        row.className = 'vh-studio-row';
        (section.items || []).forEach(item => {
            const card = document.createElement('button');
            card.type = 'button';
            card.className = 'vh-studio-card';
            card.style.backgroundImage = item.imageBackdropUrl ? 'url("' + item.imageBackdropUrl + '")' : '';
            card.textContent = item.name;
            card.addEventListener('click', () => openItem(item));
            row.appendChild(card);
        });

        block.append(title, row);
        return block;
    }

    function renderSidebar(root) {
        const sidebar = document.createElement('nav');
        sidebar.className = 'vh-sidebar';
        [
            ['Inicio', '#!/home.html'],
            ['Peliculas', '#!/movies.html'],
            ['Series', '#!/tv.html'],
            ['Colecciones', '#!/collections.html'],
            ['Favoritos', '#!/favorites.html'],
            ['Busqueda', '#!/search.html'],
            ['Configuracion', '#!/dashboard.html']
        ].forEach(([label, url]) => {
            const link = document.createElement('a');
            link.href = url;
            link.textContent = label;
            sidebar.appendChild(link);
        });
        root.appendChild(sidebar);
    }

    function renderSections(sections, clientConfig) {
        const host = findHomeHost();
        removeRoot();
        let normalizedSections = (sections || []).map(normalizeSection).sort((a, b) => a.position - b.position);
        if (!normalizedSections.some(section => section.items && section.items.length > 0)) {
            normalizedSections = buildNativeFallbackSections();
        }

        const root = document.createElement('div');
        root.id = rootId;
        root.className = 'vh-home';

        normalizedSections.forEach(section => {
            if (section.success === false) {
                return;
            }

            const node = section.visualType === 'hero'
                ? renderHero(section)
                : section.visualType === 'studioCollection'
                    ? renderStudios(section)
                    : renderRow(section);

            if (node) {
                root.appendChild(node);
            }
        });

        if (clientConfig.sidebarEnabled) {
            renderSidebar(root);
        }

        if (root.childElementCount > 0) {
            host.prepend(root);
        } else {
            renderDiagnostic('Visual Home cargo, pero no recibio secciones con items. Revisa /VisualHome/sections y los logs [VisualHome].');
        }
    }

    function buildNativeFallbackSections() {
        const cards = Array.from(document.querySelectorAll('.card, .cardBox, .posterCard, .overflowBackdropCard, [data-id]'))
            .filter(card => !card.closest('#' + rootId) && !card.closest('#' + diagnosticId));

        const seen = new Set();
        const items = [];

        cards.forEach(card => {
            const link = card.closest('a') || card.querySelector('a');
            const url = link ? (link.getAttribute('href') || '') : '';
            const id = card.getAttribute('data-id') || url || card.textContent || String(items.length);
            if (seen.has(id)) {
                return;
            }

            const img = card.querySelector('img');
            const image = img && img.src ? img.src : extractBackground(card);
            const nameNode = card.querySelector('.cardText, .cardText-first, .cardName, .itemName, .textActionButton')
                || card.querySelector('[title]')
                || card;
            const name = (nameNode.getAttribute && nameNode.getAttribute('title')) || (nameNode.textContent || '').trim();

            if (!name && !image) {
                return;
            }

            seen.add(id);
            items.push({
                id: id,
                name: name || 'Jellyfin',
                url: url,
                overview: '',
                productionYear: null,
                officialRating: '',
                imagePrimaryUrl: image,
                imageBackdropUrl: image,
                genres: []
            });
        });

        if (items.length === 0) {
            return [];
        }

        return [
            {
                sectionId: 'native-hero',
                name: 'Destacado',
                visualType: 'hero',
                position: 0,
                success: true,
                items: [items[0]]
            },
            {
                sectionId: 'native-home',
                name: 'En tu Jellyfin',
                visualType: 'carousel',
                position: 10,
                success: true,
                items: items.slice(0, 24)
            }
        ];
    }

    function extractBackground(element) {
        const candidates = [element].concat(Array.from(element.querySelectorAll('*')).slice(0, 8));
        for (const candidate of candidates) {
            const background = window.getComputedStyle(candidate).backgroundImage || '';
            const match = background.match(/url\(["']?(.*?)["']?\)/);
            if (match && match[1]) {
                return match[1];
            }
        }

        return '';
    }

    function scheduleRender() {
        window.clearTimeout(state.timer);
        state.timer = window.setTimeout(render, 250);
    }

    function render() {
        if (state.rendering) {
            return;
        }

        const path = location.pathname + location.hash;
        if (!isHomePage()) {
            removeRoot();
            state.lastPath = path;
            return;
        }

        if (!window.ApiClient || !ApiClient.ajax) {
            if (state.apiWaits < 40) {
                state.apiWaits += 1;
                window.setTimeout(scheduleRender, 250);
                return;
            }

            renderDiagnostic('Visual Home no pudo encontrar ApiClient de Jellyfin Web.');
            return;
        }

        state.apiWaits = 0;
        state.rendering = true;
        ensureStylesheet();

        Promise.all([
            api('VisualHome/client-config'),
            api(withUser('VisualHome/sections'))
        ]).then(([clientConfig, sections]) => {
            const normalizedConfig = normalizeClientConfig(clientConfig);
            if (!normalizedConfig.pluginEnabled || !normalizedConfig.visualInjectionEnabled) {
                removeRoot();
                renderDiagnostic('Visual Home esta desactivado en la configuracion del plugin.');
                return;
            }

            renderSections(sections || [], normalizedConfig);
        }).catch(error => {
            console.warn('[VisualHome] frontend render failed', error);
            renderDiagnostic('Visual Home cargo, pero fallo al consultar el backend. Revisa la consola del navegador y los logs del servidor.');
        }).finally(() => {
            state.lastPath = path;
            state.rendering = false;
        });
    }

    function watchNavigation() {
        window.addEventListener('hashchange', scheduleRender);
        window.addEventListener('popstate', scheduleRender);

        if (state.observer) {
            state.observer.disconnect();
        }

        state.observer = new MutationObserver(() => {
            if (isHomePage() && !document.getElementById(rootId)) {
                scheduleRender();
            }
        });
        state.observer.observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            watchNavigation();
            scheduleRender();
        });
    } else {
        watchNavigation();
        scheduleRender();
    }
})();
