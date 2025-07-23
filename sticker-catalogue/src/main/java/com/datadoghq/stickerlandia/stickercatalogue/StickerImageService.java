package com.datadoghq.stickerlandia.stickercatalogue;

import jakarta.enterprise.context.ApplicationScoped;
import jakarta.inject.Inject;
import java.io.InputStream;
import java.util.UUID;
import org.eclipse.microprofile.config.inject.ConfigProperty;
import software.amazon.awssdk.core.sync.RequestBody;
import software.amazon.awssdk.services.s3.S3Client;
import software.amazon.awssdk.services.s3.model.DeleteObjectRequest;
import software.amazon.awssdk.services.s3.model.GetObjectRequest;
import software.amazon.awssdk.services.s3.model.GetUrlRequest;
import software.amazon.awssdk.services.s3.model.PutObjectRequest;

/** Service for managing sticker images in AWS S3. */
@ApplicationScoped
public class StickerImageService {

    @ConfigProperty(name = "sticker.images.bucket")
    String bucketName;

    @Inject S3Client s3Client;

    /**
     * Uploads an image to S3 and returns the storage key.
     *
     * @param imageStream the input stream of the image
     * @param contentType the MIME type of the image
     * @param contentLength the size of the image in bytes
     * @return the S3 key where the image is stored
     */
    public String uploadImage(InputStream imageStream, String contentType, long contentLength) {
        String key = "stickers/" + UUID.randomUUID().toString();

        PutObjectRequest putRequest =
                PutObjectRequest.builder()
                        .bucket(bucketName)
                        .key(key)
                        .contentType(contentType)
                        .contentLength(contentLength)
                        .build();

        s3Client.putObject(putRequest, RequestBody.fromInputStream(imageStream, contentLength));

        return key;
    }

    /**
     * Retrieves an image from S3 as an input stream.
     *
     * @param key the S3 key of the image
     * @return input stream of the image
     */
    public InputStream getImage(String key) {
        GetObjectRequest getRequest =
                GetObjectRequest.builder().bucket(bucketName).key(key).build();

        return s3Client.getObject(getRequest);
    }

    /**
     * Gets the public URL for an image in S3.
     *
     * @param key the S3 key of the image
     * @return the public URL of the image
     */
    public String getImageUrl(String key) {
        GetUrlRequest getUrlRequest = GetUrlRequest.builder().bucket(bucketName).key(key).build();

        return s3Client.utilities().getUrl(getUrlRequest).toString();
    }

    /**
     * Deletes an image from S3.
     *
     * @param key the S3 key of the image to delete
     */
    public void deleteImage(String key) {
        DeleteObjectRequest deleteRequest =
                DeleteObjectRequest.builder().bucket(bucketName).key(key).build();

        s3Client.deleteObject(deleteRequest);
    }
}
