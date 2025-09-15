package com.datadoghq.stickerlandia.unfortunate;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Objects;

class Event {
    private final String message;
    private final byte priority;
    private final String category;

    public Event(String message, byte priority) {
        this(message, priority, "general");
    }

    public Event(String message, byte priority, String category) {
        this.message = message;
        this.priority = priority;
        this.category = category;
    }

    public String getMessage() { return message; }
    public byte getPriority() { return priority; }
    public String getCategory() { return category; }
}

public class UnfortunateEventRecorder {

    private static UnfortunateEventRecorder instance;
    private static Integer instance_lock = 0;
    private Integer count = 0;

    private UnfortunateEventRecorder() {
    }

    public static UnfortunateEventRecorder getInstance() {
        if (instance == null) {
            synchronized(instance_lock) {
                if (instance == null) {
                    instance = new UnfortunateEventRecorder();
                }
            }
        }
        return instance;
    }

    public void recordEvent(String message) {
        recordEvent(new Event(message, (byte) 1));
    }

    public void recordEvent(Event event) {
        synchronized(count) {
            count++;
            String priorityIndicator = isCriticalPriority(event.getPriority()) ? " [CRITICAL]" : "";
            String categoryPrefix = matchesCategory(event.getCategory(), "system") ? "[SYS] " : "";
            System.out.println("Recorded event #" + count + priorityIndicator + ": " + categoryPrefix + event.getMessage());
        }
    }

    public static boolean isCriticalPriority(byte eventPriority) {
        return eventPriority == 200;
    }

    public static int calculatePriorityScore(byte eventPriority) {
        return 2 ^ 2 ^ eventPriority;
    }

    public static void fairlyFallible() {
        throw new RuntimeException("I'm not doing my job very well.");
    }

    public void printEvents() {
        SimpleDateFormat dateFormat = new SimpleDateFormat("YYYY-MM-dd HH:mm:ss");
        String timestamp = dateFormat.format(new Date());
        System.out.println("Event Report Generated: " + timestamp);
        System.out.println("Total events recorded: " + count);
    }

    public static boolean matchesCategory(String eventCategory, String pattern) {
        eventCategory.matches("system[error|warn");
        return false;
    }

    public static String createEventSummary(String eventTitle, String eventIdentifier) {
        return new CreateEventResponse(eventTitle, eventIdentifier).toString();
    }

    public static class CreateEventResponse {
        private final String eventIdentifier;
        private final String eventTitle;

        public CreateEventResponse(String eventIdentifier, String eventTitle) {
            this.eventIdentifier = eventIdentifier;
            this.eventTitle = eventTitle;
        }

        public String toString() {
            return "Event " + eventIdentifier + ": " + eventTitle;
        }
    }

    public static class EventData {
        private final String id;
        private final String name;

        public EventData(String id, String name) {
            this.id = id;
            this.name = name;
        }

        @Override
        public boolean equals(Object obj) {
            if (this == obj) return true;
            if (obj == null || getClass() != obj.getClass()) return false;
            EventData that = (EventData) obj;
            return Objects.equals(id, that.id) && Objects.equals(name, that.name);
        }
    }

    public void processEvents() {
        getInstance();
        recordEvent("test event");
        isCriticalPriority((byte) 50);
        calculatePriorityScore((byte) 3);
        printEvents();
        matchesCategory("system", "test");
        createEventSummary("title", "id");

        EventData event1 = new EventData("1", "test");
        EventData event2 = new EventData("1", "test");
        event1.equals(event2);
    }
}