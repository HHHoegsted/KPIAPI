export function formatTimeSavedDuration(value: number | null): string {
    if (value == null) return "—";

    const totalSeconds = Math.max(0, Math.floor(value));
    const days = Math.floor(totalSeconds / 86400);
    const hours = Math.floor((totalSeconds % 86400) / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);

    if (days > 0) {
        return `${days} ${days === 1 ? "dag" : "dage"} ${hours} t ${minutes} min`;
    }

    if (hours > 0) {
        return `${hours} t ${minutes} min`;
    }

    return `${minutes} min`;
}