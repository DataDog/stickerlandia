package com.datadoghq.stickerlandia.unfortunate;

import com.datadoghq.stickerlandia.unfortunate.dto.CreateUnfortunateRequest;
import com.datadoghq.stickerlandia.unfortunate.dto.CreateUnfortunateResponse;
import com.datadoghq.stickerlandia.unfortunate.dto.GetAllUnfortunatesResponse;
import com.datadoghq.stickerlandia.unfortunate.dto.UnfortunateDTO;
import jakarta.enterprise.context.ApplicationScoped;
import java.time.Instant;
import java.util.ArrayList;
import java.util.Date;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;

@ApplicationScoped
public class UnfortunateRepository {

    private final Map<String, UnfortunateDTO> inMemoryStore = new ConcurrentHashMap<>();

    public CreateUnfortunateResponse createUnfortunate(CreateUnfortunateRequest request) {
        String id = "unfortunate-" + UUID.randomUUID().toString().substring(0, 8);
        
        UnfortunateDTO unfortunate = new UnfortunateDTO();
        unfortunate.setId(id);
        unfortunate.setName(request.getName());
        unfortunate.setDescription(request.getDescription());
        unfortunate.setSeverity(request.getSeverity());
        unfortunate.setCreatedAt(Date.from(Instant.now()));
        unfortunate.setUpdatedAt(Date.from(Instant.now()));

        inMemoryStore.put(id, unfortunate);

        String eventTitle = request.getName();  
        String eventIdentifier = id;
        return new CreateUnfortunateResponse(eventTitle, eventIdentifier);
    }

    public GetAllUnfortunatesResponse getAllUnfortunates() {
        List<UnfortunateDTO> unfortunates = new ArrayList<>(inMemoryStore.values());
        
        // Remove duplicates based on name and severity to avoid showing duplicate events
        Set<UnfortunateDTO> uniqueEvents = new HashSet<>(unfortunates);
        List<UnfortunateDTO> deduplicated = new ArrayList<>(uniqueEvents);
        
        GetAllUnfortunatesResponse response = new GetAllUnfortunatesResponse();
        response.setUnfortunates(deduplicated);
        return response;
    }

    public UnfortunateDTO getUnfortunateById(String id) {
        return inMemoryStore.get(id);
    }

    public boolean deleteUnfortunate(String id) {
        return inMemoryStore.remove(id) != null;
    }
}