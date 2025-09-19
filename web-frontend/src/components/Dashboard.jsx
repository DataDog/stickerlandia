import React, { useState, useEffect } from "react";
import HotelClassOutlinedIcon from "@mui/icons-material/HotelClassOutlined";
import TrendingUpOutlinedIcon from "@mui/icons-material/TrendingUpOutlined";
import GroupOutlinedIcon from "@mui/icons-material/GroupOutlined";
import CalendarMonthOutlinedIcon from "@mui/icons-material/CalendarMonthOutlined";
import CircleOutlinedIcon from '@mui/icons-material/CircleOutlined';
import StarBorderOutlinedIcon from '@mui/icons-material/StarBorderOutlined';
import DiamondOutlinedIcon from '@mui/icons-material/DiamondOutlined';


const Dashboard = () => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  return (
    <div className="dashboard-wrapper mx-auto w-full px-6 sm:max-w-[40rem] md:max-w-[48rem] md:px-8 lg:max-w-[64rem] xl:max-w-[80rem]">
      <div className="dashboard-greeting">
        <div className="text-3xl font-bold my-3">Public Dashboard</div>
        <div className="text-gray-600 my-3">
          Live statistics and trends from the Stickerlandia community.
        </div>
      </div>
      <div className="dashboard-info grid grid-cols-4 gap-4">
        <div className="col-span-1 landing-card items-start">
          <span className="text-gray-400 font-bold">Total Stickers</span>
          <span className="text-gray-600 font-bold text-xl">156</span>
          <span className="text-green-500">
            <TrendingUpOutlinedIcon /> +12% this week
          </span>
        </div>
        <div className="col-span-1 landing-card items-start">
          <span className="text-gray-400 font-bold">Active Users</span>
          <span className="text-gray-600 font-bold text-xl">12,847</span>
          <span className="text-green-500">
            <TrendingUpOutlinedIcon /> +347 this week
          </span>
        </div>
        <div className="col-span-1 landing-card items-start">
          <span className="text-gray-400 font-bold">Stickers Collected</span>
          <span className="text-gray-600 font-bold text-xl">89,234</span>
          <span className=" text-blue-600">
            <GroupOutlinedIcon /> 570 avg per user
          </span>
        </div>
        <div className="col-span-1 landing-card items-start">
          <span className="text-gray-400 font-bold">Events Hosted</span>
          <span className="text-gray-600 font-bold text-xl">23</span>
          <span className="text-purple-600">
            <CalendarMonthOutlinedIcon /> DASH 2025 upcoming
          </span>
        </div>
        <div className="col-span-2 landing-card items-start">
          <span className="text-gray-600 font-bold">Rarity Distribution</span>
          <span className="text-gray-600 font-bold text-xl">
            Breakdown of stickers by rarity level
          </span>
          <div className="w-full">
            <div className="rarity-wrapper grid grid-cols-2 my-3">
              <span className="font-bold col-span-1"><CircleOutlinedIcon/> Common</span>
              <div className="col-span-1 flex">
                <div className="collection-progress-bar rounded-md bg-linear-65 from-gray-800 via-gray-400 to-gray-400 to-75% block h-5 w-2/3 "></div>
                <p className="inline text-gray-600 px-5">10%</p>
              </div>
            </div>
            <div className="rarity-wrapper grid grid-cols-2 my-3">
              <span className="font-bold col-span-1"><StarBorderOutlinedIcon/> Rare</span>
              <div className="col-span-1 flex">
                <div className="collection-progress-bar rounded-md bg-linear-65 from-gray-800 via-gray-400 to-gray-400 to-75% block h-5 w-2/3 "></div>
                <p className="inline text-gray-600 px-5">30%</p>
              </div>
            </div>
            <div className="rarity-wrapper grid grid-cols-2 my-3">
              <span className="font-bold col-span-1"><DiamondOutlinedIcon/> Epic</span>
              <div className="col-span-1 flex">
                <div className="collection-progress-bar rounded-md bg-linear-65 from-gray-800 via-gray-400 to-gray-400 to-75% block h-5 w-2/3 "></div>
                <p className="inline text-gray-600 px-5">20%</p>
              </div>
            </div>
            <div className="rarity-wrapper grid grid-cols-2 my-3">
              <span className="font-bold col-span-1"><HotelClassOutlinedIcon/> Legendary</span>
              <div className="col-span-1 flex">
                <div className="collection-progress-bar rounded-md bg-linear-65 from-gray-800 via-gray-400 to-gray-400 to-75% block h-5 w-2/3 "></div>
                <p className="inline text-gray-600 px-5">40%</p>
              </div>
            </div>
          </div>
        </div>
        <div className="col-span-2 landing-card">
          <span className="text-gray-600 font-bold">Recent Activity</span>
          <span className="text-gray-600 font-bold text-xl">
            Latest sticker collections and events
          </span>
          <div className="grid grid-rows-2">
            <div className="font-bold">Dash 2024 Attendee sticker claimed</div>
            <div className="text-sm">2 minutes ago</div>
          </div>
          <div className="grid grid-rows-2">
            <div className="font-bold">New certification stickers released</div>
            <div className="text-sm">1 hour ago</div>
          </div>
          <div className="grid grid-rows-2">
            <div className="font-bold">Community milestone reached</div>
            <div className="text-sm">3 hours ago</div>
          </div>
        </div>
        <div className="col-span-4 landing-card items-start">
          <span className="text-gray-600 font-bold">Most Collected Stickers</span>
          <span className="text-gray-600 font-bold text-sm">Top Stickers in the Community</span>
          <span className="text-green-500">
            
          </span>
        </div>
      </div>
      
    </div>
  );
};

export default Dashboard;
