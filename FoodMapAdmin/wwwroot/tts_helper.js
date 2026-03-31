window.ttsHelper = {
    voices: [],
    dotNetRef: null,
    isPlaying: false,
    _heartbeat: null,

    // 1. Khởi động (Init)
    init: async function (dotNetRef) {
        this.dotNetRef = dotNetRef;
        this.voices = window.speechSynthesis.getVoices();
        
        if (this.voices.length === 0) {
            window.speechSynthesis.onvoiceschanged = () => {
                this.voices = window.speechSynthesis.getVoices();
            };
        }

        // Warm-up: Chuẩn bị sẵn engine âm thanh
        setTimeout(() => {
            ['vi', 'en', 'zh'].forEach(lang => {
                const u = new SpeechSynthesisUtterance(' ');
                u.volume = 0;
                u.voice = this.pickVoice(lang);
                window.speechSynthesis.speak(u);
            });
        }, 1000);
    },

    // 2. Chọn giọng đọc (Ưu tiên Online/Neural)
    pickVoice: function (langCode) {
        const prefs = {
            'vi': ['Nam Minh', 'Hoai My', 'Google Vietnamese', 'NamMinhNeural', 'HoaiMyNeural'],
            'en': ['Aria Online', 'AriaNeural', 'Google US English', 'David', 'Zira'],
            'zh': ['Xiaoxiao Online', 'XiaoxiaoNeural', 'Google 普通话', 'Huihui', 'Kangkang']
        };

        const list = prefs[langCode] || prefs['en'];
        
        for (const name of list) {
            const found = this.voices.find(v => v.name.includes(name));
            if (found) {
                console.log(`[TTS] Selected Voice (${langCode}): ${found.name}`);
                return found;
            }
        }

        const matched = this.voices.filter(v => v.lang.toLowerCase().startsWith(langCode));
        const fallback = matched.find(v => v.name.includes('Online') || v.name.includes('Natural') || v.name.includes('Neural'))
            || matched.find(v => v.name.includes('Google'))
            || matched[0] 
            || null;
            
        if (fallback) console.log(`[TTS] Fallback Voice (${langCode}): ${fallback.name}`);
        return fallback;
    },

    // 3. Phát âm thanh (Speak)
    async speak(text, langCode) {
        this.stop(false); // Clear old audio without notifying Blazor
        if (!text) return;

        await new Promise(r => setTimeout(r, 50));

        const cleanText = text.normalize('NFC').replace(/\s+/g, ' ').trim();
        const utterance = new SpeechSynthesisUtterance(cleanText);
        
        const voice = this.pickVoice(langCode);
        if (voice) {
            utterance.voice = voice;
            utterance.lang = voice.lang;
        } else {
            utterance.lang = langCode === 'vi' ? 'vi-VN' : (langCode === 'zh' ? 'zh-CN' : 'en-US');
        }

        utterance.rate = 1.0;
        utterance.pitch = 1.0;

        utterance.onstart = () => {
            this.isPlaying = true;
            this._startHeartbeat();
            if (this.dotNetRef) this.dotNetRef.invokeMethodAsync('OnTtsProgress', 50).catch(() => {});
        };

        utterance.onend = () => this.stop();
        utterance.onerror = () => this.stop();

        window.speechSynthesis.speak(utterance);
    },

    // 4. Tạm dừng (Pause)
    pause() {
        window.speechSynthesis.pause();
        this._stopHeartbeat();
    },

    // 5. Tiếp tục (Resume)
    resume() {
        window.speechSynthesis.resume();
        this._startHeartbeat();
    },

    // 6. Dừng hẳn (Stop)
    stop(notify = true) {
        this.isPlaying = false;
        this._stopHeartbeat();
        window.speechSynthesis.cancel();
        if (notify && this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnTtsFinished').catch(() => {});
        }
    },

    _startHeartbeat: function () {
        this._stopHeartbeat();
        this._heartbeat = setInterval(() => window.speechSynthesis.resume(), 10000);
    },

    _stopHeartbeat: function () {
        if (this._heartbeat) clearInterval(this._heartbeat);
        this._heartbeat = null;
    }
};