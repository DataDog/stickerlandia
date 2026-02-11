/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.UnitTest.PrintJobTests;

public class SubmitPrintJobCommandTests
{
    public class IsValidMethod
    {
        [Fact]
        public void WithValidData_ReturnsTrue()
        {
            var command = new SubmitPrintJobCommand
            {
                UserId = "user123",
                StickerId = "sticker456",
                StickerUrl = "https://example.com/sticker.png"
            };

            command.IsValid().Should().BeTrue();
        }

        [Fact]
        public void WithHttpUrl_ReturnsTrue()
        {
            var command = new SubmitPrintJobCommand
            {
                UserId = "user123",
                StickerId = "sticker456",
                StickerUrl = "http://example.com/sticker.png"
            };

            command.IsValid().Should().BeTrue();
        }

        [Fact]
        public void WithEmptyUserId_ReturnsFalse()
        {
            var command = new SubmitPrintJobCommand
            {
                UserId = "",
                StickerId = "sticker456",
                StickerUrl = "https://example.com/sticker.png"
            };

            command.IsValid().Should().BeFalse();
        }

        [Fact]
        public void WithNullUserId_ReturnsFalse()
        {
            var command = new SubmitPrintJobCommand
            {
                UserId = null!,
                StickerId = "sticker456",
                StickerUrl = "https://example.com/sticker.png"
            };

            command.IsValid().Should().BeFalse();
        }

        [Fact]
        public void WithEmptyStickerId_ReturnsFalse()
        {
            var command = new SubmitPrintJobCommand
            {
                UserId = "user123",
                StickerId = "",
                StickerUrl = "https://example.com/sticker.png"
            };

            command.IsValid().Should().BeFalse();
        }

        [Fact]
        public void WithNullStickerId_ReturnsFalse()
        {
            var command = new SubmitPrintJobCommand
            {
                UserId = "user123",
                StickerId = null!,
                StickerUrl = "https://example.com/sticker.png"
            };

            command.IsValid().Should().BeFalse();
        }

        [Fact]
        public void WithEmptyStickerUrl_ReturnsFalse()
        {
            var command = new SubmitPrintJobCommand
            {
                UserId = "user123",
                StickerId = "sticker456",
                StickerUrl = ""
            };

            command.IsValid().Should().BeFalse();
        }

        [Fact]
        public void WithNullStickerUrl_ReturnsFalse()
        {
            var command = new SubmitPrintJobCommand
            {
                UserId = "user123",
                StickerId = "sticker456",
                StickerUrl = null!
            };

            command.IsValid().Should().BeFalse();
        }

        [Fact]
        public void WithRelativeUrl_ReturnsFalse()
        {
            var command = new SubmitPrintJobCommand
            {
                UserId = "user123",
                StickerId = "sticker456",
                StickerUrl = "/images/sticker.png"
            };

            command.IsValid().Should().BeFalse();
        }

        [Fact]
        public void WithInvalidUrl_ReturnsFalse()
        {
            var command = new SubmitPrintJobCommand
            {
                UserId = "user123",
                StickerId = "sticker456",
                StickerUrl = "not-a-valid-url"
            };

            command.IsValid().Should().BeFalse();
        }
    }
}
