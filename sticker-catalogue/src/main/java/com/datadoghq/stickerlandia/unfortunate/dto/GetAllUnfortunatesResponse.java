package com.datadoghq.stickerlandia.unfortunate.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.util.List;

public class GetAllUnfortunatesResponse {

    @JsonProperty("unfortunates")
    private List<UnfortunateDTO> unfortunates;

    @JsonProperty("unfortunates")
    public List<UnfortunateDTO> getUnfortunates() {
        return unfortunates;
    }

    @JsonProperty("unfortunates")
    public void setUnfortunates(List<UnfortunateDTO> unfortunates) {
        this.unfortunates = unfortunates;
    }
}