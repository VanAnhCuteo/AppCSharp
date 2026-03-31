window.ttsHelper = {
    voices: [],
    dotNetRef: null,
    isPlaying: false,
    currentUtterance: null, // Giữ tham chiếu để tránh bị Garbage Collector xóa

    langMap: { 'vi': 'vi-VN', 'en': 'en-US', 'zh': 'zh-CN' },

    init: async function (dotNetRef) {
        this.dotNetRef = dotNetRef;
        this.voices = await this._getVoicesAsync();

        window.speechSynthesis.onvoiceschanged = () => {
            const newVoices = window.speechSynthesis.getVoices();
            if (newVoices.length > 0) {
                this.voices = newVoices;
            }
        };
    },

    _getVoicesAsync: function () {
        return new Promise(resolve => {
            let v = window.speechSynthesis.getVoices();
            if (v.length > 0) { resolve(v); return; }

            let attempts = 0;
            const timer = setInterval(() => {
                const refreshed = window.speechSynthesis.getVoices();
                if (refreshed.length > 0 || attempts > 20) {
                    clearInterval(timer);
                    resolve(refreshed);
                }
                attempts++;
            }, 100);
        });
    },

    _pickVoice: function (voices, langCode) {
        const prefs = {
            'vi': ['Microsoft HoaiMy Online (Natural)', 'Microsoft NamMinh Online (Natural)', 'Google Tiếng Việt', 'Microsoft An'],
            'en': ['Microsoft Aria Online (Natural)', 'Google US English'],
            'zh': ['Microsoft Xiaoxiao Online (Natural)']
        };
        const list = prefs[langCode] || prefs['en'];

        for (const name of list) {
            const found = voices.find(v => v.name.includes(name));
            if (found) return found;
        }

        const prefix = { 'vi': 'vi', 'en': 'en', 'zh': 'zh' }[langCode] || 'en';
        return voices.find(v => v.lang.toLowerCase().startsWith(prefix)) || null;
    },

    speak: function (text, langCode) {
        if (!text || !text.trim()) return;

        // 1. Dừng mọi phát âm đang chạy ngay lập tức
        this.stop();

        // 2. Làm sạch văn bản: Loại bỏ xuống dòng thừa và chuẩn hóa NFC
        const cleanText = text.replace(/[\r\n]+/g, ' ').trim().normalize('NFC');

        const mappedLang = this.langMap[langCode] || 'vi-VN';
        const voice = this._pickVoice(this.voices, langCode);

        // 3. Khởi tạo Utterance và gán vào biến object để tránh bị GC dọn dẹp
        this.currentUtterance = new SpeechSynthesisUtterance(cleanText);
        this.currentUtterance.lang = mappedLang;
        if (voice) this.currentUtterance.voice = voice;

        // Tinh chỉnh thông số để mượt hơn
        this.currentUtterance.rate = 1.0;
        this.currentUtterance.pitch = 1.0;

        this.currentUtterance.onstart = () => {
            this.isPlaying = true;
            if (this.dotNetRef) this.dotNetRef.invokeMethodAsync('OnTtsProgress', 0).catch(() => { });
        };

        this.currentUtterance.onend = () => {
            this.isPlaying = false;
            this.currentUtterance = null; // Giải phóng bộ nhớ
            if (this.dotNetRef) this.dotNetRef.invokeMethodAsync('OnTtsFinished').catch(() => { });
        };

        this.currentUtterance.onerror = (e) => {
            console.error("[TTS] Error:", e);
            this.isPlaying = false;
            this.currentUtterance = null;
            if (this.dotNetRef) this.dotNetRef.invokeMethodAsync('OnTtsFinished').catch(() => { });
        };

        // 4. Mẹo quan trọng cho Edge/Chrome: Resume trước khi Speak để "đánh thức" engine
        window.speechSynthesis.resume();

        // Delay nhẹ để đảm bảo lệnh cancel() trước đó đã hoàn tất xử lý luồng
        setTimeout(() => {
            window.speechSynthesis.speak(this.currentUtterance);
        }, 100);
    },

    stop: function () {
        this.isPlaying = false;
        if (window.speechSynthesis.speaking) {
            window.speechSynthesis.cancel();
        }
    }
};