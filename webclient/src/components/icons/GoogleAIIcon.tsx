import React from 'react';

const GoogleAIIcon = (props: React.SVGProps<SVGSVGElement>) => (
  <svg
    xmlns="http://www.w3.org/2000/svg"
    width="256"
    height="256"
    viewBox="0 0 24 24"
    {...props}
  >
    <path
      fill="currentColor"
      d="M12 15a3 3 0 1 0 0-6a3 3 0 0 0 0 6Z"
    />
    <path
      fill="currentColor"
      fillRule="evenodd"
      d="M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2S2 6.477 2 12s4.477 10 10 10Zm0-1.5a8.5 8.5 0 1 0 0-17a8.5 8.5 0 0 0 0 17Z"
      clipRule="evenodd"
    />
  </svg>
);

export default GoogleAIIcon;
