-- MySQL dump 10.13  Distrib 8.0.45, for Win64 (x86_64)
--
-- Host: localhost    Database: foodapp
-- ------------------------------------------------------
-- Server version	8.0.45

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `activity_logs`
--

DROP TABLE IF EXISTS `activity_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `activity_logs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `action_type` varchar(100) NOT NULL,
  `target_name` varchar(255) DEFAULT NULL,
  `details` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_log_user` (`user_id`),
  CONSTRAINT `fk_log_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=88 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `activity_logs`
--

LOCK TABLES `activity_logs` WRITE;
/*!40000 ALTER TABLE `activity_logs` DISABLE KEYS */;
INSERT INTO `activity_logs` VALUES (87,1,'Xóa ảnh','Quán Ốc Mai','Đã xóa một ảnh của Quán Ốc Mai','2026-04-14 14:56:32');
/*!40000 ALTER TABLE `activity_logs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `app_notifications`
--

DROP TABLE IF EXISTS `app_notifications`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `app_notifications` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `title` varchar(255) NOT NULL,
  `message` text NOT NULL,
  `type` varchar(50) DEFAULT 'info',
  `is_read` tinyint(1) DEFAULT '0',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `category` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_user_read` (`user_id`,`is_read`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `app_notifications`
--

LOCK TABLES `app_notifications` WRITE;
/*!40000 ALTER TABLE `app_notifications` DISABLE KEYS */;
INSERT INTO `app_notifications` VALUES (1,2,'Audio Guide bị từ chối','Bản thuyết minh cho quán \'Hồng Trà Chanh\' không được phê duyệt.','alert',1,'2026-04-14 01:17:18','audio'),(2,2,'Audio Guide được phê duyệt','Yêu cầu THÊM MỚI thuyết minh cho quán \'Hồng Trà Chanh\' đã được duyệt và dịch tự động.','success',1,'2026-04-14 01:29:26','audio'),(3,2,'Audio Guide được phê duyệt','Yêu cầu THÊM MỚI thuyết minh cho quán \'Hồng Trà Chanh\' đã được duyệt và dịch tự động.','success',1,'2026-04-14 01:29:27','audio'),(4,2,'Audio Guide bị từ chối','Rất tiếc, yêu cầu XÓA BỎ thuyết minh cho quán \'Hồng Trà Chanh\' đã bị từ chối.','alert',0,'2026-04-14 01:42:35','audio');
/*!40000 ALTER TABLE `app_notifications` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `categories`
--

DROP TABLE IF EXISTS `categories`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `categories` (
  `category_id` int NOT NULL AUTO_INCREMENT,
  `category_name` varchar(100) NOT NULL,
  `description` text,
  PRIMARY KEY (`category_id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `categories`
--

LOCK TABLES `categories` WRITE;
/*!40000 ALTER TABLE `categories` DISABLE KEYS */;
INSERT INTO `categories` VALUES (1,'Hải sản','Các quán hải sản'),(2,'Ăn vặt','Các món ăn vặt'),(3,'Đồ nướng','BBQ và đồ nướng');
/*!40000 ALTER TABLE `categories` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `languages`
--

DROP TABLE IF EXISTS `languages`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `languages` (
  `language_code` varchar(20) NOT NULL,
  `name` varchar(100) NOT NULL,
  `flag_url` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`language_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `languages`
--

LOCK TABLES `languages` WRITE;
/*!40000 ALTER TABLE `languages` DISABLE KEYS */;
INSERT INTO `languages` VALUES ('en','English',NULL),('ja','Japanese',NULL),('ko','Korean',NULL),('vi','Vietnamese (Tiếng Việt)',NULL),('zh','Chinese (Mandarin)',NULL);
/*!40000 ALTER TABLE `languages` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `poi_audio_logs`
--

DROP TABLE IF EXISTS `poi_audio_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `poi_audio_logs` (
  `log_id` int NOT NULL AUTO_INCREMENT,
  `poi_id` int NOT NULL,
  `user_id` int DEFAULT '1',
  `duration_seconds` int NOT NULL DEFAULT '0',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`log_id`),
  KEY `poi_id` (`poi_id`),
  CONSTRAINT `poi_audio_logs_ibfk_1` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=111 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `poi_audio_logs`
--

LOCK TABLES `poi_audio_logs` WRITE;
/*!40000 ALTER TABLE `poi_audio_logs` DISABLE KEYS */;
INSERT INTO `poi_audio_logs` VALUES (1,12,5,21,'2026-04-07 21:37:10'),(2,12,5,18,'2026-04-07 22:38:08'),(3,12,5,14,'2026-04-08 00:24:42'),(4,12,5,13,'2026-04-08 00:25:05'),(5,12,5,15,'2026-04-08 23:55:48'),(6,12,5,13,'2026-04-09 00:24:52'),(7,12,5,13,'2026-04-09 00:25:14'),(8,12,5,8,'2026-04-09 01:19:23'),(9,12,5,18,'2026-04-09 01:42:08'),(10,12,5,4,'2026-04-10 01:47:08'),(11,12,5,5,'2026-04-10 01:47:14'),(12,12,5,13,'2026-04-10 01:47:32'),(13,12,5,7,'2026-04-10 01:47:44'),(14,12,5,18,'2026-04-10 02:11:33'),(15,12,5,4,'2026-04-10 02:11:43'),(16,12,5,2,'2026-04-10 02:11:48'),(17,12,5,8,'2026-04-10 02:13:06'),(18,12,5,3,'2026-04-10 02:13:12'),(19,12,5,12,'2026-04-10 02:13:25'),(20,12,5,6,'2026-04-10 02:13:32'),(21,12,5,24,'2026-04-10 02:14:00'),(22,12,5,10,'2026-04-10 02:23:29'),(23,12,5,4,'2026-04-10 02:23:51'),(24,12,5,27,'2026-04-10 02:24:24'),(25,12,5,28,'2026-04-10 02:25:00'),(26,12,5,6,'2026-04-10 02:39:46'),(27,12,5,6,'2026-04-10 03:03:11'),(28,12,5,8,'2026-04-10 03:03:23'),(29,12,5,7,'2026-04-10 03:03:40'),(30,12,5,4,'2026-04-10 03:04:09'),(31,12,5,1,'2026-04-10 03:04:31'),(32,12,5,4,'2026-04-10 03:04:44'),(33,12,5,5,'2026-04-10 03:06:36'),(34,12,5,25,'2026-04-10 03:29:27'),(35,12,5,16,'2026-04-10 03:32:22'),(36,12,5,33,'2026-04-10 03:46:44'),(37,12,5,34,'2026-04-10 03:52:41'),(38,12,5,36,'2026-04-10 04:29:02'),(39,12,5,13,'2026-04-10 04:30:58'),(40,13,5,7,'2026-04-10 04:31:45'),(41,13,5,31,'2026-04-10 04:58:19'),(42,12,5,7,'2026-04-10 04:58:40'),(43,13,5,2,'2026-04-10 04:58:50'),(44,12,5,32,'2026-04-10 05:10:51'),(45,13,5,21,'2026-04-10 05:11:19'),(46,12,5,9,'2026-04-10 05:11:31'),(47,13,5,4,'2026-04-10 05:11:43'),(48,13,5,16,'2026-04-10 05:12:04'),(49,12,5,17,'2026-04-10 05:16:23'),(50,12,5,2,'2026-04-10 05:16:26'),(51,13,5,2,'2026-04-10 05:17:53'),(52,12,5,3,'2026-04-10 05:18:03'),(53,12,5,33,'2026-04-10 05:25:21'),(54,13,5,12,'2026-04-10 05:25:39'),(55,12,5,37,'2026-04-10 11:57:53'),(56,13,5,27,'2026-04-10 11:58:25'),(57,12,5,4,'2026-04-10 11:58:32'),(58,12,5,21,'2026-04-10 12:02:04'),(59,13,5,14,'2026-04-10 12:09:49'),(60,13,5,12,'2026-04-10 12:11:01'),(61,12,5,32,'2026-04-10 12:16:06'),(62,12,5,20,'2026-04-10 12:16:38'),(63,13,5,9,'2026-04-10 12:19:09'),(64,13,5,14,'2026-04-10 12:22:33'),(65,13,5,14,'2026-04-10 12:27:17'),(66,13,5,1,'2026-04-10 12:27:28'),(67,13,5,5,'2026-04-10 12:27:35'),(68,12,5,23,'2026-04-10 12:37:50'),(69,13,5,28,'2026-04-10 12:39:46'),(70,13,5,11,'2026-04-10 12:41:33'),(71,13,5,39,'2026-04-10 13:01:30'),(72,13,5,45,'2026-04-10 13:43:00'),(73,12,5,31,'2026-04-10 13:47:36'),(74,12,5,31,'2026-04-10 13:51:51'),(75,12,5,12,'2026-04-10 14:27:20'),(76,12,5,34,'2026-04-10 14:37:47'),(77,13,5,30,'2026-04-10 14:38:17'),(78,12,5,32,'2026-04-10 14:46:54'),(79,12,5,31,'2026-04-10 14:50:24'),(80,12,5,32,'2026-04-10 15:20:54'),(81,13,5,29,'2026-04-10 15:21:24'),(82,12,5,34,'2026-04-11 00:53:57'),(83,13,5,29,'2026-04-11 00:54:27'),(84,12,5,35,'2026-04-11 01:02:38'),(85,13,5,30,'2026-04-11 01:03:09'),(86,12,5,3,'2026-04-11 23:56:33'),(87,12,5,8,'2026-04-11 23:57:20'),(88,12,5,6,'2026-04-11 23:59:58'),(89,12,5,2,'2026-04-12 00:00:00'),(90,12,5,4,'2026-04-12 00:00:07'),(91,12,5,1,'2026-04-12 00:00:08'),(92,12,5,4,'2026-04-12 00:00:15'),(93,12,5,2,'2026-04-12 00:00:17'),(94,12,5,1,'2026-04-12 00:00:19'),(95,12,5,2,'2026-04-12 00:00:24'),(96,12,5,1,'2026-04-12 00:00:26'),(97,12,5,3,'2026-04-12 01:02:57'),(98,13,5,1,'2026-04-13 00:28:51'),(99,12,5,2,'2026-04-13 00:50:56'),(100,12,5,3,'2026-04-13 00:51:05'),(101,12,1,1,'2026-04-13 02:51:08'),(102,16,5,8,'2026-04-14 02:34:08'),(103,12,5,1,'2026-04-14 02:49:24'),(104,13,5,4,'2026-04-14 02:49:42'),(105,13,5,3,'2026-04-14 02:49:50'),(106,16,5,16,'2026-04-14 03:18:48'),(107,16,5,12,'2026-04-14 03:38:18'),(108,16,5,23,'2026-04-14 13:31:17'),(109,12,5,2,'2026-04-14 16:57:20'),(110,13,5,2,'2026-04-14 16:57:54');
/*!40000 ALTER TABLE `poi_audio_logs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `poi_guide_pending_changes`
--

DROP TABLE IF EXISTS `poi_guide_pending_changes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `poi_guide_pending_changes` (
  `change_id` int NOT NULL AUTO_INCREMENT,
  `poi_id` int NOT NULL,
  `guide_id` int DEFAULT NULL,
  `change_type` varchar(20) NOT NULL,
  `title` varchar(200) DEFAULT NULL,
  `description` text,
  `language` varchar(20) DEFAULT 'vi',
  `user_id` int DEFAULT NULL,
  `status` varchar(20) DEFAULT 'pending',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`change_id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `poi_guide_pending_changes`
--

LOCK TABLES `poi_guide_pending_changes` WRITE;
/*!40000 ALTER TABLE `poi_guide_pending_changes` DISABLE KEYS */;
INSERT INTO `poi_guide_pending_changes` VALUES (1,16,NULL,'create','menu hấp dẫn','Hồng Trà Chanh là quán nước quen thuộc với giới trẻ, nổi bật với hương vị trà đậm đà, chanh tươi thanh mát và mức giá rất dễ chịu. Không gian quán thoải mái, phù hợp để tụ tập bạn bè, trò chuyện và thư giãn sau những giờ học tập, làm việc căng thẳng.','vi',2,'approved','2026-04-13 23:51:50'),(2,16,NULL,'delete','N/A','N/A','vi',2,'rejected','2026-04-14 00:58:46'),(3,16,NULL,'delete','menu hấp dẫn','Hồng Trà Chanh là quán nước quen thuộc với giới trẻ, nổi bật với hương vị trà đậm đà, chanh tươi thanh mát và mức giá rất dễ chịu. Không gian quán thoải mái, phù hợp để tụ tập bạn bè, trò chuyện và thư giãn sau những giờ học tập, làm việc căng thẳng.','vi',2,'rejected','2026-04-14 01:38:37');
/*!40000 ALTER TABLE `poi_guide_pending_changes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `poi_guides`
--

DROP TABLE IF EXISTS `poi_guides`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `poi_guides` (
  `guide_id` int NOT NULL AUTO_INCREMENT,
  `poi_id` int DEFAULT NULL,
  `title` varchar(200) DEFAULT NULL,
  `description` text,
  `language` varchar(20) DEFAULT NULL,
  PRIMARY KEY (`guide_id`),
  KEY `poi_guides_ibfk_1` (`poi_id`),
  CONSTRAINT `poi_guides_ibfk_1` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=81 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `poi_guides`
--

LOCK TABLES `poi_guides` WRITE;
/*!40000 ALTER TABLE `poi_guides` DISABLE KEYS */;
INSERT INTO `poi_guides` VALUES (69,13,'giới thiệu quán','Menu Trà Sữa Ken rất đa dạng với nhiều lựa chọn hấp dẫn như trà sữa truyền thống, trà trái cây thanh mát và các món đá xay thơm ngon. Topping phong phú gồm trân châu, thạch, pudding giúp khách hàng dễ dàng chọn theo sở thích. Hương vị đậm đà, dễ uống, phù hợp với nhiều đối tượng. Với mức giá hợp lý và chất lượng ổn định, Trà Sữa Ken là địa điểm lý tưởng để thưởng thức và tụ họp bạn bè.','vi'),(70,13,'restaurant introduction','Ken Milk Tea menu is very diverse with many attractive options such as traditional milk tea, cool fruit tea and delicious ground ice dishes. Rich topping including pearls, jelly, pudding makes it easy for customers to choose according to their preferences. The taste is rich, easy to drink, suitable for many subjects. With reasonable price and stable quality, Ken Milk Tea is the ideal place to enjoy and gather friends.','en'),(71,13,'关于','Ken奶茶菜单非常多样化，有许多有吸引力的选择，如传统奶茶、凉爽水果茶和美味的碎冰菜肴。丰富的配料，包括珍珠、果冻、布丁，让顾客可以根据自己的喜好轻松选择。口感丰富，饮用方便，适合众多科目。肯奶茶价格合理，质量稳定，是享受和聚会的理想场所。','zh'),(72,13,'레스토랑 소개','켄 밀크티 메뉴는 전통 밀크티, 시원한 과일차, 맛있는 분쇄 얼음 요리 등 다양한 매력적인 옵션으로 매우 다양합니다. 진주, 젤리, 푸딩을 포함한 풍부한 토핑을 통해 고객은 자신의 취향에 따라 쉽게 선택할 수 있습니다. 맛은 풍부하고 마시기 쉬우며 많은 피험자에게 적합합니다. 합리적인 가격과 안정적인 품질의 켄 밀크티는 친구들과 즐기고 모일 수 있는 이상적인 장소입니다.','ko'),(73,13,'導入','ケンミルクティーのメニューは非常に多様で、伝統的なミルクティー、クールフルーツティー、美味しいグランドアイス料理など、多くの魅力的なオプションがあります。パール、ゼリー、プリンなどの豊富なトッピングにより、お客様は好みに合わせて簡単に選択できます。味わいが豊かで飲みやすく、多くの被験者に適しています。リーズナブルな価格と安定した品質のケンミルクティーは、友達と楽しんだり、集まったりするのに理想的な場所です。','ja'),(75,16,'menu hấp dẫn','Hồng Trà Chanh là quán nước quen thuộc với giới trẻ, nổi bật với hương vị trà đậm đà, chanh tươi thanh mát và mức giá rất dễ chịu. Không gian quán thoải mái, phù hợp để tụ tập bạn bè, trò chuyện và thư giãn sau những giờ học tập, làm việc căng thẳng.','vi'),(76,16,'诱人的菜单','Hồng Trà Chanh是年轻人熟悉的酒吧，具有浓郁的茶味、新鲜的柠檬和非常实惠的价格。空间舒适，适合在紧张的学习和工作时间后聚会、聊天和放松。','zh'),(77,16,'tempting menu','Hồng Trà Chanh is a bar familiar to young people, featuring a strong tea flavor, fresh lemon and a very pleasant price. The space is comfortable, suitable for gathering friends, chatting and relaxing after stressful study and work hours.','en'),(78,16,'आकर्षक मेन्यू','Hồng Trà Chanh युवा लोगों के लिए एक परिचित बार है, जिसमें एक मजबूत चाय का स्वाद, ताजा नींबू और एक बहुत ही सुखद मूल्य है। यह जगह आरामदायक है, तनावपूर्ण अध्ययन और काम के घंटों के बाद दोस्तों को इकट्ठा करने, चैट करने और आराम करने के लिए उपयुक्त है।','hi'),(79,16,'魅力的なメニュー','Hồng Trà Chanhは若者に親しまれているバーで、強い紅茶の風味、新鮮なレモン、そして非常に心地よい価格を特徴としています。快適で、ストレスの多い勉強や勤務時間の後に友達と集まり、おしゃべりをしたり、リラックスしたりするのに適しています。','ja'),(80,16,'유혹적인 메뉴','Hồng Trà Chanh 은 (는) 젊은이들에게 친숙한 바로서, 강한 차 맛, 신선한 레몬, 매우 즐거운 가격을 특징으로 합니다. 숙소는 편안하고, 친구들을 모으고, 스트레스가 많은 공부와 업무 시간 후에 수다를 떨고 휴식을 취하기에 적합합니다.','ko');
/*!40000 ALTER TABLE `poi_guides` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `poi_image_pending_changes`
--

DROP TABLE IF EXISTS `poi_image_pending_changes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `poi_image_pending_changes` (
  `change_id` int NOT NULL AUTO_INCREMENT,
  `poi_id` int DEFAULT NULL,
  `image_id` int DEFAULT NULL,
  `image_url` varchar(255) DEFAULT NULL,
  `change_type` varchar(20) NOT NULL,
  `user_id` int DEFAULT NULL,
  `status` varchar(20) DEFAULT 'pending',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`change_id`),
  KEY `fk_img_pending_poi` (`poi_id`),
  KEY `fk_img_pending_user` (`user_id`),
  CONSTRAINT `fk_img_pending_poi` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_img_pending_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `poi_image_pending_changes`
--

LOCK TABLES `poi_image_pending_changes` WRITE;
/*!40000 ALTER TABLE `poi_image_pending_changes` DISABLE KEYS */;
INSERT INTO `poi_image_pending_changes` VALUES (6,12,NULL,'/images/images (1).jpg','add',2,'approved','2026-04-13 21:58:46'),(7,12,36,'/images/images.jpg','delete',2,'rejected','2026-04-13 23:57:44');
/*!40000 ALTER TABLE `poi_image_pending_changes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `poi_images`
--

DROP TABLE IF EXISTS `poi_images`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `poi_images` (
  `image_id` int NOT NULL AUTO_INCREMENT,
  `poi_id` int DEFAULT NULL,
  `image_url` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`image_id`),
  KEY `poi_id` (`poi_id`),
  CONSTRAINT `poi_images_ibfk_1` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`)
) ENGINE=InnoDB AUTO_INCREMENT=40 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `poi_images`
--

LOCK TABLES `poi_images` WRITE;
/*!40000 ALTER TABLE `poi_images` DISABLE KEYS */;
INSERT INTO `poi_images` VALUES (1,NULL,'/images/oc1.jpg'),(2,NULL,'/images/banhtrang.jpg'),(3,NULL,'/images/oc2.jpg'),(4,NULL,'/images/oc3.jpg'),(5,NULL,'/images/oc4.jpg'),(6,NULL,'/images/banhtrang2.jpg'),(7,NULL,'/images/banhtrang3.jpg'),(8,NULL,'/images/banhtrang4.jpg'),(37,12,'/images/c23e3a27-oc-quan-2-18.jpg'),(38,14,'/images/StockAnhDep022.jpg'),(39,12,'/images/images (1).jpg');
/*!40000 ALTER TABLE `poi_images` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `poi_pending_changes`
--

DROP TABLE IF EXISTS `poi_pending_changes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `poi_pending_changes` (
  `change_id` int NOT NULL AUTO_INCREMENT,
  `poi_id` int DEFAULT NULL,
  `change_type` varchar(20) DEFAULT 'update',
  `category_id` int DEFAULT NULL,
  `name` varchar(200) DEFAULT NULL,
  `address` varchar(255) DEFAULT NULL,
  `latitude` decimal(10,7) DEFAULT NULL,
  `longitude` decimal(10,7) DEFAULT NULL,
  `open_time` varchar(50) DEFAULT NULL,
  `range_meters` int DEFAULT '50',
  `user_id` int DEFAULT NULL,
  `status` enum('pending','approved','rejected') DEFAULT 'pending',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`change_id`),
  KEY `fk_pending_poi` (`poi_id`),
  CONSTRAINT `fk_pending_poi` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `poi_pending_changes`
--

LOCK TABLES `poi_pending_changes` WRITE;
/*!40000 ALTER TABLE `poi_pending_changes` DISABLE KEYS */;
INSERT INTO `poi_pending_changes` VALUES (3,NULL,'update',1,'Hải sản 5KU','Bùi Viện Q1',10.7671645,106.6930814,'17:00 - 02:00',50,2,'approved','2026-03-31 17:41:41'),(4,NULL,'delete',1,'Hải sản 5KU','Bùi Viện Q1',10.7671645,106.6930814,'17:00 - 02:00',50,2,'approved','2026-03-31 18:18:21'),(9,16,'create',2,'Hồng Trà Chanh','C11/323E',10.7608142,106.7069006,'17:00 - 02:00',50,2,'approved','2026-04-13 15:00:48'),(10,17,'create',3,'Buffet Linh Chi','C11/323F',10.7604084,106.7033494,'16:00 - 01:00',50,2,'approved','2026-04-13 16:53:45'),(11,12,'delete',1,'Quán Ốc Mai','C11/323A',10.7610091,106.7047110,'06:00 - 18:00',44,2,'rejected','2026-04-13 16:59:10');
/*!40000 ALTER TABLE `poi_pending_changes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `poi_qrs`
--

DROP TABLE IF EXISTS `poi_qrs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `poi_qrs` (
  `qr_id` int NOT NULL AUTO_INCREMENT,
  `poi_id` int NOT NULL,
  `qr_code_url` text NOT NULL,
  PRIMARY KEY (`qr_id`),
  KEY `poi_id` (`poi_id`),
  CONSTRAINT `poi_qrs_ibfk_1` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `poi_qrs`
--

LOCK TABLES `poi_qrs` WRITE;
/*!40000 ALTER TABLE `poi_qrs` DISABLE KEYS */;
INSERT INTO `poi_qrs` VALUES (7,12,'/images/qr_03933b6f-0901-40e3-9765-ca51b01201e8.png'),(9,13,'/images/qr_c237f342-df6e-4956-b695-198340d61de8.png');
/*!40000 ALTER TABLE `poi_qrs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `pois`
--

DROP TABLE IF EXISTS `pois`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pois` (
  `poi_id` int NOT NULL AUTO_INCREMENT,
  `category_id` int DEFAULT NULL,
  `name` varchar(200) NOT NULL,
  `address` varchar(255) DEFAULT NULL,
  `latitude` decimal(10,7) DEFAULT NULL,
  `longitude` decimal(10,7) DEFAULT NULL,
  `open_time` varchar(50) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `user_id` int DEFAULT NULL,
  `range_meters` int DEFAULT '50',
  PRIMARY KEY (`poi_id`),
  KEY `category_id` (`category_id`),
  KEY `fk_pois_user` (`user_id`),
  CONSTRAINT `fk_pois_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`),
  CONSTRAINT `pois_ibfk_1` FOREIGN KEY (`category_id`) REFERENCES `categories` (`category_id`)
) ENGINE=InnoDB AUTO_INCREMENT=18 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pois`
--

LOCK TABLES `pois` WRITE;
/*!40000 ALTER TABLE `pois` DISABLE KEYS */;
INSERT INTO `pois` VALUES (12,1,'Quán Ốc Mai','C11/323A',10.7610091,106.7047110,'06:00 - 18:00','2026-04-07 14:26:42',2,44),(13,2,'Quán Trà Sữa Ken','C11/323B',10.7605875,106.7047119,'07:00 - 21:00','2026-04-08 00:26:46',3,50),(14,3,'Xiên Nướng VK','C11/323C',10.7611409,106.7024052,'08:00 - 22:00','2026-04-08 00:28:11',4,50),(15,1,'Hải sản Tươi Sống 33','C11/323D',10.7615362,106.7027391,'Cảngày','2026-04-09 22:12:55',4,5),(16,2,'Hồng Trà Chanh','C11/323E',10.7608142,106.7069006,'17:00 - 02:00','2026-04-13 15:02:19',2,50),(17,3,'Buffet Linh Chi','C11/323F',10.7604084,106.7033494,'16:00 - 01:00','2026-04-13 17:06:34',2,50);
/*!40000 ALTER TABLE `pois` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tour_pois`
--

DROP TABLE IF EXISTS `tour_pois`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tour_pois` (
  `tour_poi_id` int NOT NULL AUTO_INCREMENT,
  `tour_id` int NOT NULL,
  `poi_id` int NOT NULL,
  `sequence_order` int NOT NULL,
  `stay_duration` int DEFAULT '30' COMMENT 'Minutes',
  `average_price` decimal(15,2) DEFAULT '0.00',
  PRIMARY KEY (`tour_poi_id`),
  KEY `tour_id` (`tour_id`),
  KEY `poi_id` (`poi_id`),
  CONSTRAINT `tour_pois_ibfk_1` FOREIGN KEY (`tour_id`) REFERENCES `tours` (`tour_id`) ON DELETE CASCADE,
  CONSTRAINT `tour_pois_ibfk_2` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tour_pois`
--

LOCK TABLES `tour_pois` WRITE;
/*!40000 ALTER TABLE `tour_pois` DISABLE KEYS */;
INSERT INTO `tour_pois` VALUES (1,2,12,1,30,0.00),(2,2,15,2,30,0.00),(3,3,13,1,30,0.00),(4,3,16,2,30,0.00);
/*!40000 ALTER TABLE `tour_pois` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tours`
--

DROP TABLE IF EXISTS `tours`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tours` (
  `tour_id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) NOT NULL,
  `description` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `duration_minutes` int NOT NULL DEFAULT '60',
  `price` decimal(15,2) NOT NULL DEFAULT '0.00',
  PRIMARY KEY (`tour_id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tours`
--

LOCK TABLES `tours` WRITE;
/*!40000 ALTER TABLE `tours` DISABLE KEYS */;
INSERT INTO `tours` VALUES (2,'Tour Hải Sản','Du khách có thể trải nghiệm các món ăn độc đáo','2026-04-12 00:57:17',90,200000.00),(3,'Tour Giải Khát','Trải nghiệm các món nước siêu ngon','2026-04-13 22:38:31',30,100000.00);
/*!40000 ALTER TABLE `tours` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_locations`
--

DROP TABLE IF EXISTS `user_locations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_locations` (
  `user_id` int NOT NULL,
  `latitude` decimal(10,7) NOT NULL,
  `longitude` decimal(10,7) NOT NULL,
  `last_active` datetime NOT NULL,
  PRIMARY KEY (`user_id`),
  CONSTRAINT `user_locations_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_locations`
--

LOCK TABLES `user_locations` WRITE;
/*!40000 ALTER TABLE `user_locations` DISABLE KEYS */;
INSERT INTO `user_locations` VALUES (5,10.7607517,106.7065650,'2026-04-14 15:05:09');
/*!40000 ALTER TABLE `user_locations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `user_id` int NOT NULL AUTO_INCREMENT,
  `username` varchar(50) NOT NULL,
  `email` varchar(100) DEFAULT NULL,
  `password` varchar(255) DEFAULT NULL,
  `role` enum('user','admin','CNH') DEFAULT 'user',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `is_blocked` tinyint(1) DEFAULT '0',
  PRIMARY KEY (`user_id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'vananh','vananh@gmail.com','123456','admin','2026-03-15 02:55:03',0),(2,'chuquan1','cq1@gmail.com','123456','CNH','2026-03-20 04:59:14',0),(3,'chuquan2','cq2@gmail.com','123456','CNH','2026-03-20 04:59:14',1),(4,'chuquan3','cq3@gmail.com','123456','CNH','2026-03-20 04:59:14',0),(5,'khachhang1','khachhang1@gmail.com','123456','user','2026-03-24 09:29:03',0);
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-04-14 17:31:43
