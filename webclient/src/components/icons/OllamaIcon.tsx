import React from 'react';

const OllamaIcon = (props: React.SVGProps<SVGSVGElement>) => (
  <svg
    xmlns="http://www.w3.org/2000/svg"
    viewBox="0 0 24 24"
    width="24"
    height="24"
    {...props}
  >
    {/* Simplified llama head representation */}
    <path
      fill="currentColor"
      d="M12 2c-3.5 0-6.5 2.5-7 6v10c0 1.1.9 2 2 2h10c1.1 0 2-.9 2-2V8c-.5-3.5-3.5-6-7-6z"
    />
    {/* Ears */}
    <ellipse
      fill="currentColor"
      cx="8.5"
      cy="7"
      rx="1.5"
      ry="2.5"
      transform="rotate(-20 8.5 7)"
    />
    <ellipse
      fill="currentColor"
      cx="15.5"
      cy="7"
      rx="1.5"
      ry="2.5"
      transform="rotate(20 15.5 7)"
    />
    {/* Eyes */}
    <circle fill="#ffffff" cx="10" cy="11" r="1.5" />
    <circle fill="#ffffff" cx="14" cy="11" r="1.5" />
    <circle fill="currentColor" cx="10" cy="11" r="0.8" />
    <circle fill="currentColor" cx="14" cy="11" r="0.8" />
    {/* Nose */}
    <path
      fill="currentColor"
      d="M12 13.5c-.5 0-1 .2-1.3.5-.2.2-.2.5 0 .7.3.3.8.5 1.3.5s1-.2 1.3-.5c.2-.2.2-.5 0-.7-.3-.3-.8-.5-1.3-.5z"
    />
  </svg>
);

export default OllamaIcon;