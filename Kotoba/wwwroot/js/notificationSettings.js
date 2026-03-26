

window.notifSettings = (() => {

    let _ctx = null;

    function getCtx() {
        if (!_ctx) _ctx = new (window.AudioContext || window.webkitAudioContext)();
        // Resume if suspended (browser autoplay policy)
        if (_ctx.state === 'suspended') _ctx.resume();
        return _ctx;
    }

    /**
     * Plays a soft two-tone messenger-style chime.
     * @param {number} volume  0.0 – 1.0
     */
    function playSound(volume = 0.7) {
        const ctx = getCtx();
        const vol = Math.max(0, Math.min(1, volume));

        const masterGain = ctx.createGain();
        masterGain.gain.setValueAtTime(vol, ctx.currentTime);
        masterGain.connect(ctx.destination);

        const notes = [
            { freq: 880, startAt: 0, dur: 0.12 },  
            { freq: 1318, startAt: 0.1, dur: 0.18 },   
        ];

        notes.forEach(({ freq, startAt, dur }) => {
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();

            osc.type = 'sine';
            osc.frequency.setValueAtTime(freq, ctx.currentTime + startAt);

            gain.gain.setValueAtTime(0, ctx.currentTime + startAt);
            gain.gain.linearRampToValueAtTime(0.8, ctx.currentTime + startAt + 0.02);
            gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + startAt + dur);

            osc.connect(gain);
            gain.connect(masterGain);

            osc.start(ctx.currentTime + startAt);
            osc.stop(ctx.currentTime + startAt + dur + 0.05);
        });
    }


    /**
     * Returns the current notification permission state.
     * @returns {"default"|"granted"|"denied"}
     */
    function getPermission() {
        if (!('Notification' in window)) return 'denied';
        return Notification.permission;
    }

    /**
     * Requests browser notification permission.
     * @returns {Promise<"default"|"granted"|"denied">}
     */
    async function requestPermission() {
        if (!('Notification' in window)) return 'denied';
        const result = await Notification.requestPermission();
        return result;
    }

    /**
     * Shows a browser notification if permission is granted.
     * @param {string} title
     * @param {string} body
     */
    function showNotification(title, body) {
        if (!('Notification' in window)) return;
        if (Notification.permission !== 'granted') return;

        if (document.visibilityState === 'visible') return;

        new Notification(title, {
            body,
            icon: '/favicon.png',   // adjust to your favicon path
            badge: '/favicon.png',
            silent: true,           // we handle sound ourselves
            tag: 'kotoba-message',  // replace so we don't stack
            renotify: true,
        });
    }

    return { playSound, getPermission, requestPermission, showNotification };

})();
