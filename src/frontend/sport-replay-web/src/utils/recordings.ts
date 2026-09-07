import type { Match, Recording } from '../types';

const FAILED = 4;
const EXPIRED = 5;

export function isUsableRecording(recording: Recording) {
  return recording.status !== FAILED && recording.status !== EXPIRED;
}

export function matchHasRecording(match: Match, recordings?: Recording[]) {
  if (recordings?.some(isUsableRecording)) {
    return true;
  }
  return Boolean(match.hasRecording || (match.recordingCount ?? 0) > 0);
}
