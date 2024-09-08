window.nos2x = {
    signChallenge: async function (challenge, createdAt) {
        try {
            if (typeof window.nostr === 'undefined') {
                throw new Error('nos2x extension is not available. Please install the extension and try again.');
            }

            const event = {
                kind: 27235,
                tags: [],
                content: challenge,
                created_at: createdAt
            };

            const signedEvent = await window.nostr.signEvent(event);

            if (!signedEvent.sig || !signedEvent.pubkey || !signedEvent.id) {
                throw new Error('Invalid response from nos2x extension. Missing required fields.');
            }

            return signedEvent;
        } catch (err) {
            throw err;
        }
    },

    getPublicKey: async function () {
        try {
            if (typeof window.nostr === 'undefined') {
                throw new Error('nos2x extension is not available. Please install the extension and try again.');
            }

            const pubKey = await window.nostr.getPublicKey();

            if (!pubKey) {
                throw new Error('Failed to retrieve public key from nos2x extension.');
            }

            return pubKey;
        } catch (err) {
            throw err;
        }
    }
};
