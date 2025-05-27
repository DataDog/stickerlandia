package com.datadoghq.stickerlandia.stickeraward.sticker;

import jakarta.enterprise.context.ApplicationScoped;
import jakarta.inject.Inject;
import org.eclipse.microprofile.config.inject.ConfigProperty;
import software.amazon.awssdk.core.sync.RequestBody;
import software.amazon.awssdk.services.s3.S3Client;
import software.amazon.awssdk.services.s3.model.*;

import java.io.InputStream;
import java.time.Duration;
import java.util.UUID;

@ApplicationScoped
public class StickerImageService {

    @ConfigProperty(name = "sticker.images.bucket")
    String bucketName;

    @Inject
    S3Client s3Client;

    public String uploadImage(InputStream imageStream, String contentType, long contentLength) {
        String key = "stickers/" + UUID.randomUUID().toString();
        
        PutObjectRequest putRequest = PutObjectRequest.builder()
                .bucket(bucketName)
                .key(key)
                .contentType(contentType)
                .contentLength(contentLength)
                .build();

        s3Client.putObject(putRequest, RequestBody.fromInputStream(imageStream, contentLength));
        
        return key;
    }

    public InputStream getImage(String key) {
        GetObjectRequest getRequest = GetObjectRequest.builder()
                .bucket(bucketName)
                .key(key)
                .build();

        return s3Client.getObject(getRequest);
    }

    public String getImageUrl(String key) {
        GetUrlRequest getUrlRequest = GetUrlRequest.builder()
                .bucket(bucketName)
                .key(key)
                .build();

        return s3Client.utilities().getUrl(getUrlRequest).toString();
    }


    public void deleteImage(String key) {
        DeleteObjectRequest deleteRequest = DeleteObjectRequest.builder()
                .bucket(bucketName)
                .key(key)
                .build();

        s3Client.deleteObject(deleteRequest);
    }
}