// ==UserScript==
// @name         Jellyfin Visual Home Loader
// @namespace    Jellyfin.Plugin.VisualHome
// @version      0.1.0.2
// @description  Loads Jellyfin Visual Home assets into Jellyfin Web without modifying server web files.
// @match        http://*/web/*
// @match        https://*/web/*
// @run-at       document-idle
// @grant        none
// ==/UserScript==

(function () {
    'use strict';

    if (window.__visualHomeLoaderActive) {
        return;
    }

    window.__visualHomeLoaderActive = true;

    function basePath() {
        const index = location.pathname.indexOf('/web/');
        return index >= 0 ? location.pathname.slice(0, index) : '';
    }

    function inject() {
        if (document.querySelector('script[data-vh-main]')) {
            return;
        }

        const script = document.createElement('script');
        script.src = basePath() + '/VisualHome/assets/visualhome.js';
        script.defer = true;
        script.dataset.vhMain = 'true';
        document.documentElement.appendChild(script);
    }

    inject();
    window.addEventListener('hashchange', inject);
})();
