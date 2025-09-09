import React from "react";
import WorkspacePremiumOutlinedIcon from '@mui/icons-material/WorkspacePremiumOutlined';
import PolylineOutlinedIcon from '@mui/icons-material/PolylineOutlined';
import PrintOutlinedIcon from '@mui/icons-material/PrintOutlined';

function Landing() {
  return (
    <div className="relative isolate overflow-hidden pb-20 pt-[calc(theme(spacing.16)+theme(spacing.20))] sm:pb-32 sm:pt-[calc(theme(spacing.16)+theme(spacing.32))] -mt-16">
      <div className="landing-cta">
        <h1 className="text-purple-600 mx-auto max-w-[17.5rem] text-balance text-3xl/9 font-bold tracking-tight sm:!max-w-4xl sm:text-4.5xl md:text-5xl lg:text-6xl [@media(width>=27.5em)]:max-w-[24.25rem]">
          Collect. Share. Print.
        </h1>
        <p className="mx-auto mt-4 max-w-md text-pretty text-base/6 sm:max-w-2xl sm:text-lg">
          Stickerlandia is your gamified digital collectibles platform. Earn
          stickers through Datadog achievements, share your collection, and
          print them at events.
        </p>
        <div className="mt-8 mb-8 flex items-center justify-center gap-x-6 gap-y-3 max-sm:flex-col">
          <button className="group relative isolate inline-flex items-center justify-center overflow-hidden text-left font-medium transition duration-300 ease-[cubic-bezier(0.4,0.36,0,1)] before:duration-300 before:ease-[cubic-bezier(0.4,0.36,0,1)] before:transtion-opacity rounded-md shadow-[0_1px_theme(colors.white/0.07)_inset,0_1px_3px_theme(colors.gray.900/0.2)] before:pointer-events-none before:absolute before:inset-0 before:-z-10 before:rounded-md before:bg-gradient-to-b before:from-white/20 before:opacity-50 hover:before:opacity-100 after:pointer-events-none after:absolute after:inset-0 after:-z-10 after:rounded-md after:bg-gradient-to-b after:from-white/10 after:from-[46%] after:to-[54%] after:mix-blend-overlay text-sm h-[1.875rem] px-3 ring-1 bg-gray-900 text-white ring-gray-900">
            Start Collecting
          </button>
          <button className="group relative isolate inline-flex items-center justify-center overflow-hidden text-left font-medium transition duration-300 ease-[cubic-bezier(0.4,0.36,0,1)] before:duration-300 before:ease-[cubic-bezier(0.4,0.36,0,1)] before:transtion-opacity rounded-md shadow-[0_1px_theme(colors.white/0.07)_inset,0_1px_3px_theme(colors.gray.900/0.2)] before:pointer-events-none before:absolute before:inset-0 before:-z-10 before:rounded-md before:bg-gradient-to-b before:from-white/20 before:opacity-50 hover:before:opacity-100 after:pointer-events-none after:absolute after:inset-0 after:-z-10 after:rounded-md after:bg-gradient-to-b after:from-white/10 after:from-[46%] after:to-[54%] after:mix-blend-overlay text-sm h-[1.875rem] px-3 ring-1 bg-white text-black ring-gray-100">
            View Public Dashboard
          </button>
        </div>

        <div className="landing-wrapper flex flex-row">
          <div className="landing-card">
            <span className="card-icon">
              <WorkspacePremiumOutlinedIcon />
            </span>
          
            <p className="card-title font-bold">Earn Through Achievements</p>
            <p className="card-copy">
              Complete certifications, attend events, and participate in the
              community to earn unique stickers.
            </p>
          </div>
          <div className="landing-card">
            <span className="card-icon">
              <PolylineOutlinedIcon />
            </span>
            
            <p className="card-title font-bold">Share Your Collection</p>
            <p className="card-copy">
              Create a public profile to showcase your achievements and share
              individual stickers on social media.
            </p>
          </div>
          <div className="landing-card">
            <span className="card-icon">
              <PrintOutlinedIcon />
            </span>
            
            <p className="card-title font-bold">Print at Events</p>
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
