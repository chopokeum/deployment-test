package co.ab180.airbridge.unity;

import android.content.Intent;

import org.jetbrains.annotations.NotNull;

import java.util.ArrayList;
import java.util.List;

import co.ab180.airbridge.common.AirbridgeTrackingBlocklist;

class AirbridgeUtils {

   private static final String KEY_CONSUMED = "airbridge_consumed";

    static List<String> joinedStringToList(String joinedString, String delimiter) {
        String[] array = joinedString.split(delimiter);
        List<String> toReturn = new ArrayList<>();
        for (String element : array) {
            // Remove white space
            String trimmed = element.trim();
            if (!trimmed.isEmpty()) toReturn.add(trimmed);
        }
        return toReturn;
    }

    static List<AirbridgeTrackingBlocklist> joinedStringToAirbridgeTrackingBlocklist(String joinedString, String delimiter) {
        List<String> stringList = joinedStringToList(joinedString, delimiter);
        List<AirbridgeTrackingBlocklist> toReturn = new ArrayList<>();
        for (String str : stringList) {
            for (AirbridgeTrackingBlocklist elem : AirbridgeTrackingBlocklist.values()) {
                if (elem.getValue().equalsIgnoreCase(str)) toReturn.add(elem);
            }
        }
        return toReturn;
    }
    
    static boolean isNotNull(Object obj) {
        return (obj != null);
    }

    static String getMessage(@NotNull Throwable throwable) {
        String message = throwable.getMessage();
        return (message == null) ? "" : message;
    }
    
    static boolean isIntentConsumed(Intent intent) {
        try {
            return intent.getBooleanExtra(KEY_CONSUMED, false);
        } catch (Throwable ignored) {
            return false;
        }
    }

    static void consumeIntent(Intent intent) {
        try {
            intent.putExtra(KEY_CONSUMED, true);
        } catch (Throwable ignored) {
        }
    }
}