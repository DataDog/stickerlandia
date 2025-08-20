import React from "react";

function Landing() {
  return (
    <div className="landing-main max-w-8/10 mx-auto">
      <div className="landing-cta">
        <h1 className="mx-auto">Collect. Share. Print.</h1>
        <p>
          Stickerlandia is your gamified digital collectibles platform. Earn
          stickers through Datadog achievements, share your collection, and
          print them at events.
        </p>
        <button>Start Your Collection</button>
        <button>View Public Dashboard</button>
        <div className="landing-wrapper flex flex-row">
          <div className="landing-card">
            <p className="card-title">Earn Through Achievements</p>
            <p className="card-copy">
              Complete certifications, attend events, and participate in the
              community to earn unique stickers.
            </p>
          </div>
          <div className="landing-card">
            <p className="card-title">Share Your Collection</p>
            <p className="card-copy">
              Create a public profile to showcase your achievements and share
              individual stickers on social media.
            </p>
          </div>
          <div>
            <p className="card-title">Print at Events</p>
            <p className="card-copy">
              Turn your digital collection into physical stickers at Datadog
              events and conferences.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

export default Landing;
