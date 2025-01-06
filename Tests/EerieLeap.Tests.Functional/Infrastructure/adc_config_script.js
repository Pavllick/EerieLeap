let _trends = new Map();
let _lastValues = new Map();

function process(channel) {
    // Initialize trend for this channel if not exists
    if (!_trends.has(channel)) {
        _trends.set(channel, getRandomDouble() * 2 - 1); // Random trend between -1 and 1
    }

    // Initialize last value if not exists
    if (!_lastValues.has(channel)) {
        _lastValues.set(channel, getRandomDouble() * 3.3); // Initial value between 0 and 3.3V
    }

    // Randomly change trend sometimes
    if (getRandomDouble() < 0.1) { // 10% chance to change trend
        _trends.set(channel, getRandomDouble() * 2 - 1);
    }

    // Calculate new value with some randomness and trend
    const currentValue = _lastValues.get(channel);
    const trend = _trends.get(channel);
    const maxChange = 0.1; // Maximum change per reading
    const change = (trend * 0.8 + getRandomDouble() * 0.4 - 0.2) * maxChange;

    let newValue = currentValue + change;

    // Keep within ADC range (0 to 3.3V)
    newValue = Math.max(0, Math.min(3.3, newValue));

    // Store the new value
    _lastValues.set(channel, newValue);

    return parseFloat(newValue.toFixed(6)); // Ensure a double precision value is returned
}

function getRandomDouble() {
    let seed = Date.now() * Math.random();
    seed = (seed * 9301 + 49297) % 233280; // Linear congruential generator

    return seed / 233280;
}

function dispose() {
    _trends.clear();
    _lastValues.clear();

    console.log('Disposed resources.');
}
