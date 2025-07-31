// below is radix ui components
import { NavigationMenu } from "radix-ui";
import "./Header.css";

export default function Header() {
  return (
    <header>
        <div className="logo-temp">✨ stickerlandia</div>
        <NavigationMenu.Root>
          <NavigationMenu.List>
            <NavigationMenu.Item>
              <NavigationMenu.Link>Public Dashboard</NavigationMenu.Link>
            </NavigationMenu.Item>
            <NavigationMenu.Item>
              <NavigationMenu.Link>Sign In</NavigationMenu.Link>
            </NavigationMenu.Item>

          </NavigationMenu.List>

          <NavigationMenu.Viewport />
        </NavigationMenu.Root>
    </header>
    
  )
}
