/**
 * TTS Helper - FoodMapAdmin
 * Optimized for Edge & Chrome: Natural voice selection + 15s bug fix.
 * Voice preference: Google Natural > Microsoft Online > System local.
 */
window.ttsHelper = {
    voices: [],
    dotNetRef: null,
    isPlaying: false,
    resumeTimer: null,
    totalChunks: 0,
    currentChunkIndex: 0,
    pendingChunks: [],
    currentMappedLang: '',
    currentVoice: null,

    langMap: { 'vi': 'vi-VN', 'en': 'en-US', 'zh': 'zh-CN' },

    init: async function (dotNetRef) {
        this.dotNetRef = dotNetRef;
        this.voices = await this._getVoicesAsync();
    },

    _getVoicesAsync: function () {
        return new Promise(resolve => {
            const v = window.speechSynthesis.getVoices();
            if (v.length > 0) { resolve(v); return; }
            window.speechSynthesis.onvoiceschanged = () => {
                resolve(window.speechSynthesis.getVoices());
            };
            // Fallback 2s
            setTimeout(() => resolve(window.speechSynthesis.getVoices()), 2000);
        });
    },

    _pickVoice: function (voices, langCode) {
        // Danh sách giọng ưu tiên - Google > Microsoft Online > Microsoft local
        const prefs = {
            'vi': ['Google Vietnamese', 'Microsoft Nam Minh Online', 'vi-VN-NamMinhNeural', 'Microsoft Hoai My Online', 'vi-VN-HoaiMyNeural'],
            'en': ['Google US English', 'Microsoft Aria Online (Natural)', 'Microsoft Guy Online (Natural)', 'Microsoft David', 'Microsoft Zira'],
            'zh': ['Google Chinese (Simplified)', 'Microsoft Xiaoxiao Online (Natural)', 'Microsoft Yunxi Online (Natural)']
        };
        const list = prefs[langCode] || prefs['en'];
        
        for (const name of list) {
            const found = voices.find(v => v.name === name || v.name.includes(name));
            if (found) return found;
        }

        // Fallback
        const prefix = { 'vi': 'vi', 'en': 'en', 'zh': 'zh' }[langCode] || 'en';
        const matched = voices.filter(v => v.lang.toLowerCase().startsWith(prefix));
        return matched.length > 0 ? matched[0] : null;
    },

    _splitText: function (text) {
        if (!text || !text.trim()) return [];
        const sentences = text.match(/[^.!?。！？\n,，;；]+[.!?。！？\n,，;；]?/g) || [text];
        const chunks = [];
        let buffer = '';

        for (const s of sentences) {
            const t = s.trim();
            if (!t) continue;
            const newBuffer = buffer ? buffer + ' ' + t : t;
            if (newBuffer.length >= 120) {
                if (buffer) { chunks.push(buffer); buffer = t; }
                else { chunks.push(newBuffer); buffer = ''; }
            } else { buffer = newBuffer; }
        }
        if (buffer.trim()) chunks.push(buffer.trim());
        return chunks;
    },

    _startKeepAlive: function () {
        this._stopKeepAlive();
        this.resumeTimer = setInterval(() => {
            if (this.isPlaying && window.speechSynthesis.speaking) {
                window.speechSynthesis.pause();
                window.speechSynthesis.resume();
            }
        }, 10000);
    },

    _stopKeepAlive: function () {
        if (this.resumeTimer) { clearInterval(this.resumeTimer); this.resumeTimer = null; }
    },

    speak: async function (text, langCode) {
        if (!text || !text.trim()) return;
        console.log(`[TTS] Speaking in "${langCode}": ${text.substring(0, 50)}...`);
        this.stop();

        if (this.voices.length === 0) {
            console.log('[TTS] Voices empty, fetching...');
            this.voices = await this._getVoicesAsync();
        }
        
        this.currentMappedLang = this.langMap[langCode] || 'vi-VN';
        this.currentVoice = this._pickVoice(this.voices, langCode);
        
        if (this.currentVoice) {
            console.log(`[TTS] Selected voice: ${this.currentVoice.name} (${this.currentVoice.lang})`);
        } else {
            console.warn(`[TTS] No perfect voice found for ${langCode}, relying on browser default for ${this.currentMappedLang}`);
        }

        const chunks = this._splitText(text);
        if (chunks.length === 0) return;

        this.isPlaying = true;
        this.totalChunks = chunks.length;
        this.currentChunkIndex = 0;
        this.pendingChunks = chunks;

        this._startKeepAlive();
        this._speakNext();
    },

    _speakNext: function () {
        if (!this.isPlaying || this.currentChunkIndex >= this.pendingChunks.length) {
            if (this.isPlaying) {
                this.isPlaying = false;
                this._stopKeepAlive();
                this._notifyFinished();
            }
            return;
        }

        const chunk = this.pendingChunks[this.currentChunkIndex];
        const utt = new SpeechSynthesisUtterance(chunk);
        utt.lang = this.currentMappedLang;
        if (this.currentVoice) utt.voice = this.currentVoice;
        
        utt.rate = 1.0;
        utt.pitch = 1.0;
        utt.volume = 1.0;

        utt.onstart = () => { this._notifyProgress(); };
        utt.onend = () => {
            if (!this.isPlaying) return;
            this.currentChunkIndex++;
            this._speakNext();
        };
        utt.onerror = (e) => {
            if (e.error === 'interrupted' || e.error === 'canceled') return;
            console.warn('TTS error:', e.error);
            this.currentChunkIndex++;
            setTimeout(() => this._speakNext(), 150);
        };

        window.speechSynthesis.speak(utt);
    },

    stop: function () {
        this.isPlaying = false;
        this._stopKeepAlive();
        window.speechSynthesis.cancel();
        this.pendingChunks = [];
        this.currentChunkIndex = 0;
        this.totalChunks = 0;
        this._notifyProgress(0);
    },

    _notifyProgress: function (override) {
        if (!this.dotNetRef) return;
        const pct = override !== undefined
            ? override
            : (this.totalChunks > 1 ? Math.round((this.currentChunkIndex / this.totalChunks) * 100) : 0);
        this.dotNetRef.invokeMethodAsync('OnTtsProgress', pct).catch(() => { });
    },

    _notifyFinished: function () {
        if (this.dotNetRef) this.dotNetRef.invokeMethodAsync('OnTtsFinished').catch(() => { });
    }
};