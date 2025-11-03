export interface ServiceProps {
  jdbcUrl: string;
  dbUsername: string;
  dbPassword: string;
  kafkaBootstrapServers: string;
  kafkaUsername: string;
  kafkaPassword: string;
}