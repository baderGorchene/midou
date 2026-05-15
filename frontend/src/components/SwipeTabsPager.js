import React from "react";
import { Dimensions, ScrollView, View, StyleSheet } from "react-native";

export default function SwipeTabsPager({
  routes,
  activeRouteName,
  onChangeRouteName,
  renderRoute,
}) {
  const width = Dimensions.get("window").width;
  const activeIndex = Math.max(0, routes.indexOf(activeRouteName));

  const scrollRef = React.useRef(null);

  React.useEffect(() => {
    scrollRef.current?.scrollTo({
      x: activeIndex * width,
      animated: true,
    });
  }, [activeIndex, width]);

  return (
    <View style={styles.container}>
      <ScrollView
        ref={scrollRef}
        horizontal
        pagingEnabled
        showsHorizontalScrollIndicator={false}
        scrollEventThrottle={16}
        onMomentumScrollEnd={(e) => {
          const index = Math.round(e.nativeEvent.contentOffset.x / width);
          const nextRoute = routes[index];
          if (nextRoute && nextRoute !== activeRouteName) {
            onChangeRouteName?.(nextRoute);
          }
        }}
      >
        {routes.map((routeName) => (
          <View key={routeName} style={{ width, flex: 1 }}>
            {renderRoute(routeName)}
          </View>
        ))}
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
});