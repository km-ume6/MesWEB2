// --- getInputValue.js ---
window.getInputValue = (input) => {
    if (!input) return "";

    const el = input instanceof HTMLElement ? input : document.getElementById(input);
    if (!el) return "";

    return el.value;
}

// --- insertAtCursorAndFocus.js ---
window.insertAtCursorAndFocus = (input, symbol) => {
    if (!input) return;

    const el = input instanceof HTMLElement ? input : document.getElementById(input);
    if (!el) return;

    const start = el.selectionStart;
    const end = el.selectionEnd;
    const value = el.value;

    el.value = value.substring(0, start) + symbol + value.substring(end);
    el.selectionStart = el.selectionEnd = start + symbol.length;
    el.focus();
}

// --- autocomplete.js ---
window.autocomplete = (function () {
    return {
        init: function (elementId, suggestions) {
            try {
                var el = document.getElementById(elementId);
                if (!el) return;
                // If Awesomplete is not loaded, do nothing
                if (typeof Awesomplete === 'undefined') return;

                // If instance already exists, update list
                if (el.awesomplete) {
                    el.awesomplete.list = suggestions || [];
                } else {
                    // Create new instance
                    el.awesomplete = new Awesomplete(el, {
                        list: suggestions || [],
                        minChars: 0,
                        maxItems: 10,
                        autoFirst: true,
                        replace: function(suggestion) {
                            // Custom replace function to ensure the input gets the suggestion text
                            this.input.value = suggestion;
                        }
                    });

                    // Show suggestions on focus
                    el.addEventListener('focus', function () {
                        if (el.value === '') {
                            el.awesomplete.evaluate();
                        }
                    });

                    // Show suggestions on click (for mobile)
                    el.addEventListener('click', function () {
                        if (el.value === '') {
                            el.awesomplete.evaluate();
                        }
                    });

                    // Handle selection completion
                    el.addEventListener('awesomplete-selectcomplete', function (e) {
                        console.log('Android Autocomplete: selectcomplete event for', elementId, 'value:', el.value);
                        
                        // Manually trigger multiple events to ensure Blazor recognizes the change
                        var events = ['input', 'change', 'blur'];
                        events.forEach(eventType => {
                            var event = new Event(eventType, { 
                                bubbles: true, 
                                cancelable: true 
                            });
                            el.dispatchEvent(event);
                        });
                        
                        // Force a small delay and trigger again to ensure Blazor processes
                        setTimeout(() => {
                            el.dispatchEvent(new Event('input', { bubbles: true }));
                            console.log('Android Autocomplete: delayed input event fired for', elementId);
                        }, 100);
                    });

                    // Also handle the select event (before completion)
                    el.addEventListener('awesomplete-select', function (e) {
                        console.log('Android Autocomplete: select event for', elementId, 'selection:', e.text);
                        
                        // Set value immediately
                        setTimeout(() => {
                            el.value = e.text.value || e.text;
                            el.dispatchEvent(new Event('input', { bubbles: true }));
                            el.dispatchEvent(new Event('change', { bubbles: true }));
                            console.log('Android Autocomplete: value set to', el.value);
                        }, 50);
                    });

                    // For additional safety, monitor value changes and ensure Blazor is notified
                    var lastValue = el.value;
                    setInterval(() => {
                        if (el.value !== lastValue) {
                            console.log('Android Autocomplete: value changed detected for', elementId, 'from', lastValue, 'to', el.value);
                            lastValue = el.value;
                            el.dispatchEvent(new Event('input', { bubbles: true }));
                        }
                    }, 500);
                }
            } catch (e) {
                console.error('autocomplete.init error', e);
            }
        },
        update: function (elementId, suggestions) {
            var el = document.getElementById(elementId);
            if (!el || !el.awesomplete) return;
            el.awesomplete.list = suggestions || [];
        },
        destroy: function (elementId) {
            var el = document.getElementById(elementId);
            if (!el || !el.awesomplete) return;
            // Awesomplete doesn't provide destroy; remove reference
            el.awesomplete = null;
        }
    };
})();

// --- device-detection.js ---
window.deviceDetection = {
    getUserAgent: function() {
        return navigator.userAgent;
    },
    
    detectDevice: function() {
        const userAgent = navigator.userAgent.toLowerCase();
        const platform = navigator.platform.toLowerCase();
        const vendor = navigator.vendor.toLowerCase();
        
        // iOS detection (iPhone, iPad)
        const isIOS = /iphone|ipad|ipod/.test(userAgent) || 
                      /iphone|ipad|ipod/.test(platform) ||
                      (vendor.includes('apple') && !userAgent.includes('android'));
        
        // Android detection
        const isAndroid = /android/.test(userAgent) || 
                          /android/.test(platform);

        return {
            userAgent: navigator.userAgent,
            platform: navigator.platform,
            vendor: navigator.vendor,
            isIOS: isIOS,
            isAndroid: isAndroid,
            isMobile: /mobi|android|iphone|ipad|blackberry|opera mini|iemobile/i.test(userAgent),
            screen: {
                width: screen.width,
                height: screen.height
            },
            window: {
                width: window.innerWidth,
                height: window.innerHeight
            }
        };
    }
};

// --- file-download.js ---
window.downloadFileFromBase64 = function(base64String, fileName, contentType) {
    try {
        // Base64文字列をバイト配列に変換
    const byteCharacters = atob(base64String);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
      byteNumbers[i] = byteCharacters.charCodeAt(i);
     }
        const byteArray = new Uint8Array(byteNumbers);
        
        // Blobを作成
        const blob = new Blob([byteArray], { type: contentType || 'application/octet-stream' });
        
        // ダウンロードリンクを作成してクリック
        const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
     document.body.appendChild(link);
 link.click();
        
        // クリーンアップ
     setTimeout(() => {
 document.body.removeChild(link);
       URL.revokeObjectURL(url);
        }, 100);
        
        return true;
    } catch (error) {
        console.error('ファイルダウンロードエラー:', error);
        return false;
    }
};

// ExcelCompare.razor で使用される downloadFile 関数を追加
window.downloadFile = function(fileName, base64String) {
    return window.downloadFileFromBase64(base64String, fileName, 'text/csv;charset=utf-8');
};

// --- client-logger.js ---
window.clientLogger = (function () {
    const getClientLogUrl = () => {
        const baseHref = document.querySelector('base')?.getAttribute('href') ?? '/';
        const normalizedBase = baseHref.endsWith('/') ? baseHref : `${baseHref}/`;
        return `${normalizedBase}client-log`;
    };

    const send = async (payload) => {
        try {
            // Try navigator.sendBeacon first for reliability on page unload
            const data = typeof payload === 'string' ? payload : JSON.stringify(payload);
            const url = getClientLogUrl();
            if (navigator && typeof navigator.sendBeacon === 'function') {
                const blob = new Blob([data], { type: 'application/json' });
                navigator.sendBeacon(url, blob);
                return true;
            }
            // Fallback to fetch
            await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: data,
                keepalive: true
            });
            return true;
        } catch (e) {
            try {
                console.warn('clientLogger send failed', e);
            } catch { }
            return false;
        }
    };

    const format = (level, message, extra) => ({
        timestamp: new Date().toISOString(),
        level: level,
        message: message,
        userAgent: navigator.userAgent,
        url: location.href,
        extra: extra || null
    });

    // Hook global error handlers
    window.addEventListener('error', function (ev) {
        try {
            const msg = ev.message || 'error';
            const source = ev.filename || null;
            const lineno = ev.lineno || null;
            const colno = ev.colno || null;
            const err = ev.error ? (ev.error.stack || ev.error.toString()) : null;
            const payload = format('error', msg, { source, lineno, colno, stack: err });
            send(payload);
        } catch (e) { }
    });

    window.addEventListener('unhandledrejection', function (ev) {
        try {
            const reason = ev.reason ? (ev.reason.stack || ev.reason.toString()) : String(ev.reason);
            const payload = format('unhandledrejection', reason);
            send(payload);
        } catch (e) { }
    });

    // Expose API
    return {
        log: function (level, message, extra) {
            try {
                const payload = format(level || 'info', message, extra);
                return send(payload);
            } catch (e) { return false; }
        },
        info: function (message, extra) { return this.log('info', message, extra); },
        warn: function (message, extra) { return this.log('warn', message, extra); },
        error: function (message, extra) { return this.log('error', message, extra); }
    };
})();

// --- navigation helpers ---
window.navigation = window.navigation || {};

window.navigation.getOptimalNavWidth = function() {
    const viewportWidth = window.innerWidth;
    
    // モバイル (768px以下): 固定幅 260px
    if (viewportWidth <= 768) {
        return 260;
    }
    
    // タブレット (769px - 1024px): ビューポートの25%、最小220px、最大300px
    if (viewportWidth <= 1024) {
        return Math.max(220, Math.min(300, Math.floor(viewportWidth * 0.25)));
    }
    
    // ノートPC (1025px - 1440px): ビューポートの20%、最小280px、最大340px
    if (viewportWidth <= 1440) {
        return Math.max(280, Math.min(340, Math.floor(viewportWidth * 0.20)));
    }
    
    // デスクトップ (1441px - 1920px): ビューポートの18%、最小320px、最大380px
    if (viewportWidth <= 1920) {
        return Math.max(320, Math.min(380, Math.floor(viewportWidth * 0.18)));
    }
    
    // 大画面 (1920px超): ビューポートの15%、最小360px、最大420px
    return Math.max(360, Math.min(420, Math.floor(viewportWidth * 0.15)));
};

window.navigation.applyOptimalNavWidth = function() {
    const width = window.navigation.getOptimalNavWidth();
    const navPane = document.querySelector('.nav-pane');
    const mainContent = document.querySelector('.main-content');
    
    if (navPane && window.innerWidth > 768) {
        navPane.style.width = `${width}px`;
        if (mainContent) {
            mainContent.style.marginLeft = `${width}px`;
        }
    } else if (navPane && window.innerWidth <= 768) {
        // モバイルの場合はスタイルをクリア（CSSで制御）
        navPane.style.width = '';
        if (mainContent) {
            mainContent.style.marginLeft = '';
        }
    }
    
    return width;
};

window.navigation.setupResponsiveNav = function() {
    // 初期設定
    window.navigation.applyOptimalNavWidth();
    
    // リサイズイベントのデバウンス処理
    let resizeTimer;
    window.addEventListener('resize', function() {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(function() {
            window.navigation.applyOptimalNavWidth();
        }, 150);
    });
};
